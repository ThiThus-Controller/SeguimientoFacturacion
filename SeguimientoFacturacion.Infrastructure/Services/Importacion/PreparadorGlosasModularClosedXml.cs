using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure
    .Services.Importacion;

/// <summary>
/// Valida y transforma una plantilla modular de glosas
/// en objetos preparados en memoria.
/// </summary>
public sealed class
    PreparadorGlosasModularClosedXml :
        IPreparadorGlosasModular
{
    private readonly IValidadorGlosasModular
        _validador;

    private readonly IInspectorEstructuraPlantilla
        _inspector;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    /// <summary>
    /// Inicializa el preparador modular de glosas.
    /// </summary>
    public PreparadorGlosasModularClosedXml(
        IValidadorGlosasModular validador,
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
    public async Task<ResultadoPreparacionGlosasDto>
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
                "El archivo de glosas no puede prepararse " +
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
                TipoImportacion.Glosas,
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

        var glosas =
            PrepararGlosas(
                filas,
                indiceReferencias,
                cancellationToken);

        if (glosas.Count !=
            validacion.GlosasDetectadas)
        {
            throw new InvalidOperationException(
                "La cantidad de glosas preparadas no " +
                "coincide con la cantidad validada.");
        }

        if (glosas.Count(
                glosa =>
                    glosa.TieneRespuesta) !=
            validacion.GlosasConRespuestaDetectadas)
        {
            throw new InvalidOperationException(
                "La cantidad de glosas con respuesta no " +
                "coincide con la cantidad validada.");
        }

        return new ResultadoPreparacionGlosasDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            Glosas = glosas
        };
    }

    private static IReadOnlyCollection<
        FilaGlosaPreparacion> LeerFilas(
            IXLWorksheet hoja,
            ResultadoInspeccionPlantillaDto inspeccion,
            CancellationToken cancellationToken)
    {
        var filas =
            new List<FilaGlosaPreparacion>();

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

    private static FilaGlosaPreparacion LeerFila(
        IXLWorksheet hoja,
        int numeroFila,
        IReadOnlyDictionary<string, int> columnas)
    {
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

        var identificadorFe =
            NormalizarIdentificador(
                ObtenerIdentificadorFe(
                    hoja,
                    numeroFila,
                    columnas,
                    prefijo,
                    numeroFactura));

        var fechaGlosa =
            ObtenerFechaRequerida(
                hoja,
                numeroFila,
                columnas,
                "FECHA GLOSA");

        var valorGlosa =
            ObtenerDecimalRequerido(
                hoja,
                numeroFila,
                columnas,
                "VALOR GLOSA");

        var fechaRespuesta =
            ObtenerFechaOpcional(
                hoja,
                numeroFila,
                columnas,
                "FECHA RTA GLOSA");

        var estado =
            ObtenerEstadoGlosa(
                hoja,
                numeroFila,
                columnas);

        var valorAceptado =
            ObtenerDecimalOpcional(
                hoja,
                numeroFila,
                columnas,
                "VALOR ACEPTADO") ??
            decimal.Zero;

        return new FilaGlosaPreparacion(
            HojaOrigen: hoja.Name,
            NumeroFila: numeroFila,
            IdentificadorFe: identificadorFe,
            Prefijo: prefijo,
            NumeroFactura: numeroFactura,
            FechaGlosa: fechaGlosa,
            ValorGlosa: valorGlosa,
            FechaRespuesta: fechaRespuesta,
            Estado: estado,
            ValorAceptado: valorAceptado);
    }

    private static IReadOnlyCollection<
        GlosaPreparadaImportacionDto> PrepararGlosas(
            IEnumerable<FilaGlosaPreparacion> filas,
            IReadOnlyDictionary<
                string,
                ReferenciaFacturaImportacionDto>
                referencias,
            CancellationToken cancellationToken)
    {
        var glosas =
            new List<GlosaPreparadaImportacionDto>();

        foreach (var fila in filas)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (!referencias.TryGetValue(
                    fila.IdentificadorFe,
                    out var factura))
            {
                throw new InvalidOperationException(
                    "La factura relacionada con la glosa " +
                    $"de la fila {fila.NumeroFila} dejó de " +
                    "estar disponible después de validar " +
                    "el archivo.");
            }

            glosas.Add(
                new GlosaPreparadaImportacionDto
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

                    FechaGlosa =
                        fila.FechaGlosa,

                    ValorGlosa =
                        fila.ValorGlosa,

                    FechaRespuesta =
                        fila.FechaRespuesta,

                    Estado =
                        fila.Estado,

                    ValorAceptado =
                        fila.ValorAceptado
                });
        }

        return glosas;
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

    private static string ObtenerIdentificadorFe(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string prefijo,
        string numeroFactura)
    {
        var celda =
            hoja.Cell(fila, columnas["FE"]);

        var valor =
            celda.CachedValue
                .ToString()
                .Trim();

        if (!string.IsNullOrWhiteSpace(valor))
        {
            return valor;
        }

        if (!string.IsNullOrWhiteSpace(
                celda.FormulaA1))
        {
            return $"{prefijo}{numeroFactura}";
        }

        throw new InvalidOperationException(
            "La columna FE se encuentra vacía después " +
            "de validar el archivo.");
    }

    private static EstadoGlosa ObtenerEstadoGlosa(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas)
    {
        var texto =
            ObtenerTextoRequerido(
                hoja,
                fila,
                columnas,
                "ESTADO GLOSA");

        var normalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(texto);

        return normalizado switch
        {
            "1" or "ABIERTA" => EstadoGlosa.Abierta,
            "2" or "RESPONDIDA" => EstadoGlosa.Respondida,
            "3" or "ACEPTADA" => EstadoGlosa.Aceptada,
            "4" or "LEVANTADA" => EstadoGlosa.Levantada,
            "5" or "CONCILIADA" => EstadoGlosa.Conciliada,
            "7" or "EN NEGOCIACION" =>
                EstadoGlosa.EnNegociacion,

            _ => throw new InvalidOperationException(
                "El estado de la glosa no pudo convertirse " +
                "después de validar el archivo.")
        };
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

    private static DateOnly? ObtenerFechaOpcional(
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
            return null;
        }

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

    private static decimal? ObtenerDecimalOpcional(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(
                fila,
                columnas[nombreColumna]);

        if (string.IsNullOrWhiteSpace(
                celda.CachedValue
                    .ToString()
                    .Trim()))
        {
            return null;
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

    private sealed record FilaGlosaPreparacion(
        string HojaOrigen,
        int NumeroFila,
        string IdentificadorFe,
        string Prefijo,
        string NumeroFactura,
        DateOnly FechaGlosa,
        decimal ValorGlosa,
        DateOnly? FechaRespuesta,
        EstadoGlosa Estado,
        decimal ValorAceptado);
}
