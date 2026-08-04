using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure
    .Services.Importacion;

/// <summary>
/// Valida, lee y agrupa una plantilla modular de pagos
/// en objetos preparados en memoria.
/// </summary>
public sealed class PreparadorPagosModularClosedXml :
    IPreparadorPagosModular
{
    private readonly IValidadorPagosModular
        _validador;

    private readonly IInspectorEstructuraPlantilla
        _inspector;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    /// <summary>
    /// Inicializa el preparador modular de pagos.
    /// </summary>
    public PreparadorPagosModularClosedXml(
        IValidadorPagosModular validador,
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
    public async Task<ResultadoPreparacionPagosDto>
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
                "El archivo de pagos no puede prepararse " +
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
                TipoImportacion.Pagos,
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
                    .Select(
                        fila =>
                            fila.IdentificadorFe)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                cancellationToken);

        ValidarReferenciasDuplicadas(referencias);

        var indiceReferencias =
            referencias.ToDictionary(
                referencia =>
                    referencia.FacturaId,
                StringComparer.OrdinalIgnoreCase);

        var filasResueltas =
            ResolverFilas(
                filas,
                indiceReferencias,
                cancellationToken);

        var pagos =
            PrepararPagos(
                filasResueltas,
                cancellationToken);

        ValidarPostcondiciones(
            pagos,
            validacion);

        return new ResultadoPreparacionPagosDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            Pagos = pagos
        };
    }

    private static IReadOnlyCollection<
        FilaPagoPreparacion> LeerFilas(
            IXLWorksheet hoja,
            ResultadoInspeccionPlantillaDto inspeccion,
            CancellationToken cancellationToken)
    {
        var filas =
            new List<FilaPagoPreparacion>();

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

    private static FilaPagoPreparacion LeerFila(
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

        var recibo =
            NormalizarIdentificador(
                ObtenerTextoRequerido(
                    hoja,
                    numeroFila,
                    columnas,
                    "RECIBO"));

        var fechaPago =
            ObtenerFechaRequerida(
                hoja,
                numeroFila,
                columnas,
                "FECHA DE PAGO");

        var valorPagado =
            ObtenerDecimalRequerido(
                hoja,
                numeroFila,
                columnas,
                "VALOR PAGADO");

        var valorCruzado =
            ObtenerDecimalOpcionalCero(
                hoja,
                numeroFila,
                columnas,
                "VALOR CRUZADO");

        var retencion =
            ObtenerDecimalOpcionalCero(
                hoja,
                numeroFila,
                columnas,
                "RETENCION");

        var reteIca =
            ObtenerDecimalOpcionalCero(
                hoja,
                numeroFila,
                columnas,
                "RETE ICA");

        var saldoFavor =
            ObtenerDecimalOpcionalCero(
                hoja,
                numeroFila,
                columnas,
                "SALDO FAVOR");

        var saldoCruzadoPendiente =
            ObtenerDecimalOpcionalCero(
                hoja,
                numeroFila,
                columnas,
                "SALDO RETENCION");

        var valorAplicado =
            ObtenerDecimalRequerido(
                hoja,
                numeroFila,
                columnas,
                "VR PAGADO");

        var valorCruzadoAplicado =
            ObtenerDecimalOpcionalCero(
                hoja,
                numeroFila,
                columnas,
                "VR CRUZADO");

        var notas =
            ObtenerTextoOpcional(
                hoja,
                numeroFila,
                columnas,
                "NOTAS");

        return new FilaPagoPreparacion(
            HojaOrigen: hoja.Name,
            NumeroFila: numeroFila,
            IdentificadorFe: identificadorFe,
            Prefijo: prefijo,
            NumeroFactura: numeroFactura,
            FechaPago: fechaPago,
            Recibo: recibo,
            ValorPagado: valorPagado,
            ValorCruzado: valorCruzado,
            Retencion: retencion,
            ReteIca: reteIca,
            SaldoFavor: saldoFavor,
            SaldoCruzadoPendiente:
                saldoCruzadoPendiente,
            ValorAplicado: valorAplicado,
            ValorCruzadoAplicado:
                valorCruzadoAplicado,
            Notas: notas);
    }

    private static IReadOnlyCollection<
        FilaPagoResuelta> ResolverFilas(
            IEnumerable<FilaPagoPreparacion> filas,
            IReadOnlyDictionary<
                string,
                ReferenciaFacturaImportacionDto>
                referencias,
            CancellationToken cancellationToken)
    {
        var resultado =
            new List<FilaPagoResuelta>();

        foreach (var fila in filas)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (!referencias.TryGetValue(
                    fila.IdentificadorFe,
                    out var factura))
            {
                throw new InvalidOperationException(
                    "La factura relacionada con el pago " +
                    $"de la fila {fila.NumeroFila} dejó de " +
                    "estar disponible después de validar " +
                    "el archivo.");
            }

            resultado.Add(
                new FilaPagoResuelta(
                    HojaOrigen:
                        fila.HojaOrigen,

                    NumeroFila:
                        fila.NumeroFila,

                    IdentificadorFe:
                        fila.IdentificadorFe,

                    Prefijo:
                        fila.Prefijo,

                    NumeroFactura:
                        fila.NumeroFactura,

                    AseguradoraId:
                        factura.AseguradoraId,

                    FechaPago:
                        fila.FechaPago,

                    Recibo:
                        fila.Recibo,

                    ValorPagado:
                        fila.ValorPagado,

                    ValorCruzado:
                        fila.ValorCruzado,

                    Retencion:
                        fila.Retencion,

                    ReteIca:
                        fila.ReteIca,

                    SaldoFavor:
                        fila.SaldoFavor,

                    SaldoCruzadoPendiente:
                        fila.SaldoCruzadoPendiente,

                    ValorAplicado:
                        fila.ValorAplicado,

                    ValorCruzadoAplicado:
                        fila.ValorCruzadoAplicado,

                    Notas:
                        fila.Notas));
        }

        return resultado;
    }

    private static IReadOnlyCollection<
        PagoPreparadoImportacionDto> PrepararPagos(
            IEnumerable<FilaPagoResuelta> filas,
            CancellationToken cancellationToken)
    {
        var grupos =
            filas
                .GroupBy(
                    fila =>
                        new ClavePago(
                            fila.AseguradoraId,
                            fila.Recibo))
                .OrderBy(
                    grupo =>
                        grupo.Min(
                            fila =>
                                fila.NumeroFila));

        var pagos =
            new List<PagoPreparadoImportacionDto>();

        foreach (var grupo in grupos)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var filasPago =
                grupo
                    .OrderBy(
                        fila =>
                            fila.NumeroFila)
                    .ToArray();

            var referencia =
                filasPago[0];

            var aplicaciones =
                filasPago
                    .Select(
                        fila =>
                            new
                                AplicacionPagoPreparadaImportacionDto
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

                                ValorAplicado =
                                    fila.ValorAplicado,

                                ValorCruzadoAplicado =
                                    fila
                                        .ValorCruzadoAplicado
                            })
                    .ToArray();

            pagos.Add(
                new PagoPreparadoImportacionDto
                {
                    AseguradoraId =
                        referencia.AseguradoraId,

                    FechaPago =
                        referencia.FechaPago,

                    Recibo =
                        referencia.Recibo,

                    ValorPagado =
                        referencia.ValorPagado,

                    ValorCruzado =
                        referencia.ValorCruzado,

                    Retencion =
                        referencia.Retencion,

                    ReteIca =
                        referencia.ReteIca,

                    SaldoFavorReportado =
                        referencia.SaldoFavor,

                    SaldoCruzadoPendienteReportado =
                        referencia
                            .SaldoCruzadoPendiente,

                    Notas =
                        referencia.Notas,

                    Aplicaciones =
                        aplicaciones
                });
        }

        return pagos;
    }

    private static void ValidarPostcondiciones(
        IReadOnlyCollection<
            PagoPreparadoImportacionDto> pagos,
        ResultadoValidacionPagosDto validacion)
    {
        if (pagos.Count !=
            validacion.PagosDetectados)
        {
            throw new InvalidOperationException(
                "La cantidad de pagos preparados no " +
                "coincide con la cantidad validada.");
        }

        var totalAplicaciones =
            pagos.Sum(
                pago =>
                    pago.Aplicaciones.Count);

        if (totalAplicaciones !=
            validacion.AplicacionesDetectadas)
        {
            throw new InvalidOperationException(
                "La cantidad de aplicaciones preparadas " +
                "no coincide con la cantidad validada.");
        }

        if (pagos.Any(
                pago =>
                    !pago.TieneCuadreFinanciero ||
                    !pago.TieneCuadreSaldoFavor ||
                    !pago
                        .TieneCuadreSaldoCruzadoPendiente))
        {
            throw new InvalidOperationException(
                "Uno o más pagos preparados perdieron " +
                "el cuadre financiero validado.");
        }
    }

    private static void ValidarReferenciasDuplicadas(
        IEnumerable<ReferenciaFacturaImportacionDto>
            referencias)
    {
        var referenciaDuplicada =
            referencias
                .GroupBy(
                    referencia =>
                        referencia.FacturaId,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(
                    grupo =>
                        grupo.Count() > 1);

        if (referenciaDuplicada is not null)
        {
            throw new InvalidOperationException(
                "La consulta de facturas devolvió " +
                "identificadores duplicados.");
        }
    }

    private static string ObtenerTextoRequerido(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var valor =
            hoja
                .Cell(
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

    private static string? ObtenerTextoOpcional(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var valor =
            hoja
                .Cell(
                    fila,
                    columnas[nombreColumna])
                .CachedValue
                .ToString()
                .Trim();

        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor;
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

    private static decimal ObtenerDecimalOpcionalCero(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(
                fila,
                columnas[nombreColumna]);

        var texto =
            celda.CachedValue
                .ToString()
                .Trim();

        if (string.IsNullOrWhiteSpace(texto))
        {
            return decimal.Zero;
        }

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
                    hoja
                        .Cell(fila, columna)
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

        var copia = new MemoryStream();

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

    private sealed record FilaPagoPreparacion(
        string HojaOrigen,
        int NumeroFila,
        string IdentificadorFe,
        string Prefijo,
        string NumeroFactura,
        DateOnly FechaPago,
        string Recibo,
        decimal ValorPagado,
        decimal ValorCruzado,
        decimal Retencion,
        decimal ReteIca,
        decimal SaldoFavor,
        decimal SaldoCruzadoPendiente,
        decimal ValorAplicado,
        decimal ValorCruzadoAplicado,
        string? Notas);

    private sealed record FilaPagoResuelta(
        string HojaOrigen,
        int NumeroFila,
        string IdentificadorFe,
        string Prefijo,
        string NumeroFactura,
        int AseguradoraId,
        DateOnly FechaPago,
        string Recibo,
        decimal ValorPagado,
        decimal ValorCruzado,
        decimal Retencion,
        decimal ReteIca,
        decimal SaldoFavor,
        decimal SaldoCruzadoPendiente,
        decimal ValorAplicado,
        decimal ValorCruzadoAplicado,
        string? Notas);

    private sealed record ClavePago(
        int AseguradoraId,
        string Recibo);
}