using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Valida y transforma una plantilla modular de notas
/// crédito y débito en objetos preparados en memoria.
/// </summary>
public sealed class
    PreparadorNotasFacturaModularClosedXml :
    IPreparadorNotasFacturaModular
{
    private readonly IValidadorNotasFacturaModular
        _validador;

    private readonly IInspectorEstructuraPlantilla
        _inspector;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    /// <summary>
    /// Inicializa el preparador modular de notas.
    /// </summary>
    public PreparadorNotasFacturaModularClosedXml(
        IValidadorNotasFacturaModular validador,
        IInspectorEstructuraPlantilla inspector,
        IConsultaReferenciasFacturasImportacion
            consultaFacturas)
    {
        ArgumentNullException.ThrowIfNull(validador);
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(
            consultaFacturas);

        _validador = validador;
        _inspector = inspector;
        _consultaFacturas = consultaFacturas;
    }

    /// <inheritdoc />
    public async Task<
        ResultadoPreparacionNotasFacturaDto>
        PrepararAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(
            solicitud.Contenido);

        cancellationToken.ThrowIfCancellationRequested();

        var validacion =
            await _validador.ValidarAsync(
                solicitud,
                cancellationToken);

        if (!validacion.EsValido)
        {
            throw new InvalidOperationException(
                "El archivo de notas no puede prepararse " +
                $"porque contiene {validacion.TotalErrores} " +
                "error(es) bloqueante(s).");
        }

        await using var contenidoLocal =
            await CopiarContenidoAsync(
                solicitud.Contenido,
                cancellationToken);

        var inspeccion =
            await _inspector.InspeccionarAsync(
                solicitud.NombreArchivo,
                contenidoLocal,
                TipoImportacion.NotasFactura,
                cancellationToken);

        if (!inspeccion.EsValida)
        {
            throw new InvalidOperationException(
                "La estructura del archivo cambió después " +
                "de haber superado la validación.");
        }

        contenidoLocal.Position = 0;

        using var libro =
            new XLWorkbook(contenidoLocal);

        var hoja =
            libro.Worksheets.SingleOrDefault(
                elemento =>
                    string.Equals(
                        elemento.Name,
                        inspeccion.NombreHojaDatos,
                        StringComparison.Ordinal));

        if (hoja is null)
        {
            throw new InvalidOperationException(
                "La hoja validada no existe en el archivo.");
        }

        var filas =
            LeerFilas(
                hoja,
                inspeccion,
                cancellationToken);

        var referencias =
            await _consultaFacturas.ObtenerPorIdsAsync(
                filas
                    .Select(fila =>
                        fila.IdentificadorFe)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                cancellationToken);

        var indiceReferencias =
            referencias.ToDictionary(
                referencia =>
                    referencia.FacturaId,
                StringComparer.OrdinalIgnoreCase);

        var notas =
            PrepararNotas(
                filas,
                indiceReferencias,
                cancellationToken);

        return new ResultadoPreparacionNotasFacturaDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            Notas = notas
        };
    }

    private static IReadOnlyCollection<
        FilaNotaPreparacion> LeerFilas(
            IXLWorksheet hoja,
            ResultadoInspeccionPlantillaDto inspeccion,
            CancellationToken cancellationToken)
    {
        var filas =
            new List<FilaNotaPreparacion>();

        for (var numeroFila =
                 inspeccion.PrimeraFilaDatos;
             numeroFila <=
                 inspeccion.UltimaFilaUtilizada;
             numeroFila++)
        {
            if (numeroFila % 256 == 0)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();
            }

            if (!EsFilaConDatos(
                    hoja,
                    numeroFila,
                    inspeccion.Columnas.Values))
            {
                continue;
            }

            filas.Add(
                LeerFila(
                    hoja,
                    numeroFila,
                    inspeccion.Columnas));
        }

        return filas;
    }

    private static FilaNotaPreparacion LeerFila(
        IXLWorksheet hoja,
        int numeroFila,
        IReadOnlyDictionary<string, int> columnas)
    {
        var identificadorFe =
            NormalizarIdentificador(
                ObtenerTextoRequerido(
                    hoja,
                    numeroFila,
                    columnas,
                    "FE"));

        var prefijo =
            NormalizarIdentificador(
                ObtenerTextoRequerido(
                    hoja,
                    numeroFila,
                    columnas,
                    "PREFIJO"));

        var numeroFactura =
            NormalizarIdentificador(
                ObtenerTextoRequerido(
                    hoja,
                    numeroFila,
                    columnas,
                    "FACTURA"));

        var tipo =
            ConversorTipoNotaFacturaImportacion
                .Convertir(
                    ObtenerTextoRequerido(
                        hoja,
                        numeroFila,
                        columnas,
                        "TIPO NOTA"));

        var fechaNota =
            ObtenerFechaRequerida(
                hoja,
                numeroFila,
                columnas,
                "FECHA NOTA");

        var numeroNota =
            NormalizarIdentificador(
                ObtenerTextoRequerido(
                    hoja,
                    numeroFila,
                    columnas,
                    "NUMERO NOTA"));

        var valorNota =
            ObtenerDecimalRequerido(
                hoja,
                numeroFila,
                columnas,
                "VALOR NOTA");

        return new FilaNotaPreparacion(
            hoja.Name,
            numeroFila,
            identificadorFe,
            prefijo,
            numeroFactura,
            tipo,
            fechaNota,
            numeroNota,
            valorNota);
    }

    private static IReadOnlyCollection<
        NotaFacturaPreparadaImportacionDto>
        PrepararNotas(
            IEnumerable<FilaNotaPreparacion> filas,
            IReadOnlyDictionary<
                string,
                ReferenciaFacturaImportacionDto>
                referencias,
            CancellationToken cancellationToken)
    {
        var notas =
            new List<
                NotaFacturaPreparadaImportacionDto>();

        foreach (var fila in filas)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (!referencias.TryGetValue(
                    fila.IdentificadorFe,
                    out var factura))
            {
                throw new InvalidOperationException(
                    "La factura relacionada con la nota " +
                    $"de la fila {fila.NumeroFila} dejó de " +
                    "estar disponible después de validar " +
                    "el archivo.");
            }

            notas.Add(
                new NotaFacturaPreparadaImportacionDto
                {
                    HojaOrigen =
                        fila.HojaOrigen,

                    FilaOrigen =
                        fila.NumeroFila,

                    IdentificadorFe =
                        fila.IdentificadorFe,

                    Prefijo =
                        fila.Prefijo,

                    NumeroFactura =
                        fila.NumeroFactura,

                    AseguradoraId =
                        factura.AseguradoraId,

                    Tipo =
                        fila.Tipo,

                    FechaNota =
                        fila.FechaNota,

                    NumeroNota =
                        fila.NumeroNota,

                    ValorNota =
                        fila.ValorNota
                });
        }

        return notas;
    }

    private static string ObtenerTextoRequerido(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var valor =
            hoja.Cell(
                    fila,
                    columnas[nombreColumna])
                .CachedValue
                .ToString()
                .Trim();

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"La columna {nombreColumna} se encuentra " +
                "vacía después de validar el archivo.");
        }

        return valor;
    }

    private static DateOnly ObtenerFechaRequerida(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(
                fila,
                columnas[nombreColumna]);

        if (IntentarObtenerFecha(
                celda,
                out var fecha))
        {
            return fecha;
        }

        throw new InvalidOperationException(
            $"La columna {nombreColumna} no pudo " +
            "convertirse después de validar el archivo.");
    }

    private static decimal ObtenerDecimalRequerido(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(
                fila,
                columnas[nombreColumna]);

        if (IntentarObtenerDecimal(
                celda,
                out var valor))
        {
            return valor;
        }

        throw new InvalidOperationException(
            $"La columna {nombreColumna} no pudo " +
            "convertirse después de validar el archivo.");
    }

    private static bool IntentarObtenerFecha(
        IXLCell celda,
        out DateOnly fecha)
    {
        if (celda.TryGetValue<DateTime>(
                out var fechaHora))
        {
            fecha =
                DateOnly.FromDateTime(fechaHora);

            return true;
        }

        var texto =
            celda.CachedValue
                .ToString()
                .Trim();

        if (DateOnly.TryParse(
                texto,
                CultureInfo.GetCultureInfo("es-CO"),
                DateTimeStyles.AllowWhiteSpaces,
                out fecha))
        {
            return true;
        }

        return DateOnly.TryParse(
            texto,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out fecha);
    }

    private static bool IntentarObtenerDecimal(
        IXLCell celda,
        out decimal valor)
    {
        if (celda.TryGetValue<decimal>(
                out valor))
        {
            return true;
        }

        var texto =
            celda.CachedValue
                .ToString()
                .Trim();

        if (decimal.TryParse(
                texto,
                NumberStyles.Number |
                NumberStyles.AllowCurrencySymbol,
                CultureInfo.GetCultureInfo("es-CO"),
                out valor))
        {
            return true;
        }

        return decimal.TryParse(
            texto,
            NumberStyles.Number |
            NumberStyles.AllowCurrencySymbol,
            CultureInfo.InvariantCulture,
            out valor);
    }

    private static bool EsFilaConDatos(
        IXLWorksheet hoja,
        int fila,
        IEnumerable<int> columnas)
    {
        return columnas.Any(
            columna =>
                !string.IsNullOrWhiteSpace(
                    hoja.Cell(fila, columna)
                        .CachedValue
                        .ToString()
                        .Trim()));
    }

    private static string NormalizarIdentificador(
        string valor)
    {
        return valor
            .Trim()
            .ToUpperInvariant();
    }

    private static async Task<MemoryStream>
        CopiarContenidoAsync(
            Stream contenido,
            CancellationToken cancellationToken)
    {
        if (!contenido.CanRead)
        {
            throw new ArgumentException(
                "El contenido del archivo no puede leerse.",
                nameof(contenido));
        }

        var posicionOriginal =
            contenido.CanSeek
                ? contenido.Position
                : (long?)null;

        var copia =
            new MemoryStream();

        try
        {
            if (contenido.CanSeek)
            {
                contenido.Position = 0;
            }

            await contenido.CopyToAsync(
                copia,
                cancellationToken);

            copia.Position = 0;

            return copia;
        }
        catch
        {
            await copia.DisposeAsync();
            throw;
        }
        finally
        {
            if (posicionOriginal.HasValue)
            {
                contenido.Position =
                    posicionOriginal.Value;
            }
        }
    }

    private sealed record FilaNotaPreparacion(
        string HojaOrigen,
        int NumeroFila,
        string IdentificadorFe,
        string Prefijo,
        string NumeroFactura,
        TipoNotaFactura Tipo,
        DateOnly FechaNota,
        string NumeroNota,
        decimal ValorNota);
}