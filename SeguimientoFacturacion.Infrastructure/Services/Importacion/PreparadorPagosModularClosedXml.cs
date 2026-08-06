using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Prepara pagos y calcula en memoria su distribución entre
/// cartera y anticipos. La base de datos se vuelve a consultar
/// durante el procesamiento definitivo.
/// </summary>
public sealed class PreparadorPagosModularClosedXml :
    IPreparadorPagosModular
{
    private readonly IValidadorPagosModular _validador;
    private readonly IInspectorEstructuraPlantilla _inspector;
    private readonly IConsultaReferenciasFacturasImportacion
        _consultaFacturas;
    private readonly CalculadoraDistribucionPago _calculadora;

    public PreparadorPagosModularClosedXml(
        IValidadorPagosModular validador,
        IInspectorEstructuraPlantilla inspector,
        IConsultaReferenciasFacturasImportacion consultaFacturas,
        CalculadoraDistribucionPago? calculadora = null)
    {
        _validador = validador ??
            throw new ArgumentNullException(nameof(validador));
        _inspector = inspector ??
            throw new ArgumentNullException(nameof(inspector));
        _consultaFacturas = consultaFacturas ??
            throw new ArgumentNullException(nameof(consultaFacturas));
        _calculadora = calculadora ?? new CalculadoraDistribucionPago();
    }

    public async Task<ResultadoPreparacionPagosDto> PrepararAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(solicitud.Contenido);

        var validacion = await _validador.ValidarAsync(
            solicitud,
            cancellationToken);

        if (!validacion.EsValido)
        {
            throw new InvalidOperationException(
                "El archivo de pagos contiene errores bloqueantes.");
        }

        await using var contenido = await CopiarContenidoAsync(
            solicitud.Contenido,
            cancellationToken);

        var inspeccion = await _inspector.InspeccionarAsync(
            solicitud.NombreArchivo,
            contenido,
            TipoImportacion.Pagos,
            cancellationToken);

        if (!inspeccion.EsValida)
        {
            throw new InvalidOperationException(
                "La estructura del archivo cambió después de validarse.");
        }

        contenido.Position = 0;
        using var libro = new XLWorkbook(contenido);
        var nombreHoja = inspeccion.NombreHojaDatos ??
            throw new InvalidOperationException(
                "La inspección no identificó la hoja de datos.");
        var hoja = libro.Worksheet(nombreHoja);
        var filas = LeerFilas(hoja, inspeccion, cancellationToken);

        var referencias = await _consultaFacturas.ObtenerPorIdsAsync(
            filas.Select(x => x.IdentificadorFe)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            cancellationToken);

        var indice = referencias.ToDictionary(
            x => x.FacturaId,
            StringComparer.OrdinalIgnoreCase);

        var pagosAplicadosEnArchivo =
            new Dictionary<string, decimal>(
                StringComparer.OrdinalIgnoreCase);
        var filasCalculadas = new List<FilaCalculada>();

        foreach (var fila in filas.OrderBy(x => x.NumeroFila))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!indice.TryGetValue(fila.IdentificadorFe, out var factura))
            {
                throw new InvalidOperationException(
                    $"La factura de la fila {fila.NumeroFila} dejó de existir.");
            }

            pagosAplicadosEnArchivo.TryGetValue(
                factura.FacturaId,
                out var aplicadoEnArchivo);

            var distribucion = _calculadora.Distribuir(
                factura.EstadoId,
                factura.ValorFactura,
                factura.TotalNotasDebito,
                factura.TotalNotasCredito,
                factura.TotalPagosAplicados + aplicadoEnArchivo,
                fila.ValorPagado);

            pagosAplicadosEnArchivo[factura.FacturaId] =
                aplicadoEnArchivo + distribucion.ValorAplicado;

            filasCalculadas.Add(new FilaCalculada(
                fila,
                factura.AseguradoraId,
                distribucion.ValorAplicado,
                distribucion.ValorAnticipo,
                distribucion.SaldoAntes,
                distribucion.SaldoDespues,
                distribucion.FacturaAnulada,
                distribucion.FacturaMuertaPorNotaCredito));
        }

        var pagos = filasCalculadas
            .GroupBy(x => new ClavePago(x.AseguradoraId, x.Fila.Recibo))
            .OrderBy(g => g.Min(x => x.Fila.NumeroFila))
            .Select(CrearPago)
            .ToArray();

        if (pagos.Count() != validacion.PagosDetectados ||
            pagos.Sum(x => x.Aplicaciones.Count) !=
                validacion.AplicacionesDetectadas ||
            pagos.Any(x => !x.EstaDistribuido))
        {
            throw new InvalidOperationException(
                "La preparación no conserva las cantidades o la distribución validada.");
        }

        return new ResultadoPreparacionPagosDto
        {
            NombreArchivo = solicitud.NombreArchivo.Trim(),
            Pagos = pagos
        };
    }

    private static PagoPreparadoImportacionDto CrearPago(
        IGrouping<ClavePago, FilaCalculada> grupo)
    {
        var filas = grupo.OrderBy(x => x.Fila.NumeroFila).ToArray();
        var primera = filas[0];

        return new PagoPreparadoImportacionDto
        {
            AseguradoraId = grupo.Key.AseguradoraId,
            FechaPago = primera.Fila.FechaPago,
            Recibo = grupo.Key.Recibo,
            ValorPagado = filas.Sum(x => x.Fila.ValorPagado),
            Retencion = filas.Sum(x => x.Fila.Retencion),
            ReteIca = filas.Sum(x => x.Fila.ReteIca),
            Notas = primera.Fila.Notas,
            Aplicaciones = filas.Select(x =>
                new AplicacionPagoPreparadaImportacionDto
                {
                    HojaOrigen = x.Fila.HojaOrigen,
                    FilaOrigen = x.Fila.NumeroFila,
                    IdentificadorFe = x.Fila.IdentificadorFe,
                    Prefijo = x.Fila.Prefijo,
                    NumeroFactura = x.Fila.NumeroFactura,
                    ValorRecibido = x.Fila.ValorPagado,
                    ValorAplicado = x.ValorAplicado,
                    ValorAnticipo = x.ValorAnticipo,
                    SaldoAntes = x.SaldoAntes,
                    SaldoDespues = x.SaldoDespues,
                    FacturaAnulada = x.FacturaAnulada,
                    FacturaMuertaPorNotaCredito =
                        x.FacturaMuertaPorNotaCredito
                }).ToArray()
        };
    }

    private static IReadOnlyCollection<FilaPago> LeerFilas(
        IXLWorksheet hoja,
        ResultadoInspeccionPlantillaDto inspeccion,
        CancellationToken cancellationToken)
    {
        var filas = new List<FilaPago>();
        for (var numero = inspeccion.PrimeraFilaDatos;
             numero <= inspeccion.UltimaFilaUtilizada;
             numero++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!inspeccion.Columnas.Values.Any(columna =>
                    !string.IsNullOrWhiteSpace(
                        hoja.Cell(numero, columna).CachedValue.ToString())))
            {
                continue;
            }

            filas.Add(new FilaPago(
                hoja.Name,
                numero,
                Texto(hoja, numero, inspeccion.Columnas, "FE").ToUpperInvariant(),
                Texto(hoja, numero, inspeccion.Columnas, "PREFIJO").ToUpperInvariant(),
                Texto(hoja, numero, inspeccion.Columnas, "FACTURA").ToUpperInvariant(),
                Fecha(hoja, numero, inspeccion.Columnas, "FECHA DE PAGO"),
                Texto(hoja, numero, inspeccion.Columnas, "RECIBO").ToUpperInvariant(),
                Decimal(hoja, numero, inspeccion.Columnas, "VALOR PAGADO", false),
                Decimal(hoja, numero, inspeccion.Columnas, "RETENCION", true),
                Decimal(hoja, numero, inspeccion.Columnas, "RETE ICA", true),
                TextoOpcional(hoja, numero, inspeccion.Columnas, "NOTAS")));
        }

        return filas;
    }

    private static string Texto(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string columna) =>
        hoja.Cell(fila, columnas[columna]).CachedValue.ToString().Trim();

    private static string? TextoOpcional(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string columna)
    {
        var valor = Texto(hoja, fila, columnas, columna);
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private static DateOnly Fecha(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string columna)
    {
        var celda = hoja.Cell(fila, columnas[columna]);
        if (celda.TryGetValue<DateTime>(out var fechaHora))
        {
            return DateOnly.FromDateTime(fechaHora);
        }

        if (DateOnly.TryParse(
                celda.CachedValue.ToString(),
                CultureInfo.GetCultureInfo("es-CO"),
                DateTimeStyles.AllowWhiteSpaces,
                out var fecha))
        {
            return fecha;
        }

        throw new InvalidOperationException(
            $"La fecha de la fila {fila} dejó de ser válida.");
    }

    private static decimal Decimal(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string columna,
        bool permiteVacio)
    {
        var celda = hoja.Cell(fila, columnas[columna]);
        var texto = celda.CachedValue.ToString().Trim();
        if (permiteVacio && string.IsNullOrWhiteSpace(texto))
        {
            return 0m;
        }

        if (celda.TryGetValue<decimal>(out var valor) ||
            decimal.TryParse(
                texto,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.GetCultureInfo("es-CO"),
                out valor) ||
            decimal.TryParse(
                texto,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out valor))
        {
            return valor;
        }

        throw new InvalidOperationException(
            $"El importe de la fila {fila} dejó de ser válido.");
    }

    private static async Task<MemoryStream> CopiarContenidoAsync(
        Stream origen,
        CancellationToken cancellationToken)
    {
        if (origen.CanSeek)
        {
            origen.Position = 0;
        }

        var destino = new MemoryStream();
        await origen.CopyToAsync(destino, cancellationToken);
        destino.Position = 0;
        return destino;
    }

    private sealed record FilaPago(
        string HojaOrigen,
        int NumeroFila,
        string IdentificadorFe,
        string Prefijo,
        string NumeroFactura,
        DateOnly FechaPago,
        string Recibo,
        decimal ValorPagado,
        decimal Retencion,
        decimal ReteIca,
        string? Notas);

    private sealed record FilaCalculada(
        FilaPago Fila,
        int AseguradoraId,
        decimal ValorAplicado,
        decimal ValorAnticipo,
        decimal SaldoAntes,
        decimal SaldoDespues,
        bool FacturaAnulada,
        bool FacturaMuertaPorNotaCredito);

    private sealed record ClavePago(int AseguradoraId, string Recibo);
}
