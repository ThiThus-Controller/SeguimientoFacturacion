using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Realiza el análisis estructural de una plantilla
/// modular de facturas.
/// </summary>
public sealed class
    LectorEstructuralFacturasModularClosedXml :
    ILectorArchivoFacturacion
{
    private readonly IInspectorEstructuraPlantilla
        _inspectorEstructura;

    /// <summary>
    /// Inicializa el lector estructural modular.
    /// </summary>
    public LectorEstructuralFacturasModularClosedXml(
        IInspectorEstructuraPlantilla inspectorEstructura)
    {
        ArgumentNullException.ThrowIfNull(
            inspectorEstructura);

        _inspectorEstructura =
            inspectorEstructura;
    }

    /// <inheritdoc />
    public async Task<ResultadoAnalisisImportacionDto>
        AnalizarAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(
            solicitud.Contenido);

        cancellationToken.ThrowIfCancellationRequested();

        await using var contenidoLocal =
            await CopiarContenidoAsync(
                solicitud.Contenido,
                cancellationToken);

        var inspeccion =
            await _inspectorEstructura
                .InspeccionarAsync(
                    solicitud.NombreArchivo,
                    contenidoLocal,
                    TipoImportacion.Facturas,
                    cancellationToken);

        if (!inspeccion.EsValida)
        {
            return CrearResultadoInvalido(
                solicitud.NombreArchivo,
                inspeccion);
        }

        contenidoLocal.Position = 0;

        using var libro =
            new XLWorkbook(contenidoLocal);

        var hoja =
            libro.Worksheets.Single(
                elemento =>
                    string.Equals(
                        elemento.Name,
                        inspeccion.NombreHojaDatos,
                        StringComparison.Ordinal));

        var resultadoFilas =
            AnalizarFilas(
                hoja,
                inspeccion.Columnas,
                inspeccion.UltimaFilaUtilizada,
                cancellationToken);

        var inconsistencias =
            inspeccion.Inconsistencias.ToList();

        if (resultadoFilas.TotalFilas == 0)
        {
            inconsistencias.Add(
                new InconsistenciaImportacionDto
                {
                    Fila =
                        ContratosPlantillasImportacion
                            .PrimeraFilaDatos,

                    Columna = "ARCHIVO",
                    Codigo = "PLANTILLA_SIN_DATOS",

                    Mensaje =
                        "La plantilla contiene los encabezados " +
                        "correctos, pero no tiene filas de datos.",

                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                });
        }

        return new ResultadoAnalisisImportacionDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

            AniosDetectados =
                resultadoFilas.AniosDetectados,

            TotalFilasAnalizadas =
                resultadoFilas.TotalFilas,

            FacturasDetectadas =
                resultadoFilas.FacturasDetectadas,

            MovimientosDetectados = 0,
            CatalogosNoMapeados = 0,

            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static ResultadoFilas
        AnalizarFilas(
            IXLWorksheet hoja,
            IReadOnlyDictionary<string, int> columnas,
            int ultimaFila,
            CancellationToken cancellationToken)
    {
        var totalFilas = 0;
        var facturasDetectadas = 0;

        var aniosDetectados =
            new SortedSet<int>();

        var columnaFe =
            columnas["FE"];

        var columnaPrefijo =
            columnas["PREFIJO"];

        var columnaFactura =
            columnas["FACTURA"];

        var columnaFechaFactura =
            columnas["FECHA FACTURA"];

        for (var fila =
                 ContratosPlantillasImportacion
                     .PrimeraFilaDatos;
             fila <= ultimaFila;
             fila++)
        {
            if (fila % 256 == 0)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();
            }

            if (!EsFilaConDatos(
                    hoja,
                    fila,
                    columnas.Values))
            {
                continue;
            }

            totalFilas++;

            if (TieneIdentificacionFactura(
                    hoja,
                    fila,
                    columnaFe,
                    columnaPrefijo,
                    columnaFactura))
            {
                facturasDetectadas++;
            }

            if (IntentarObtenerFecha(
                    hoja.Cell(
                        fila,
                        columnaFechaFactura),
                    out var fechaFactura))
            {
                aniosDetectados.Add(
                    fechaFactura.Year);
            }
        }

        return new ResultadoFilas(
            totalFilas,
            facturasDetectadas,
            aniosDetectados.ToArray());
    }

    private static bool EsFilaConDatos(
        IXLWorksheet hoja,
        int fila,
        IEnumerable<int> columnas)
    {
        return columnas.Any(
            columna =>
                !string.IsNullOrWhiteSpace(
                    ObtenerTexto(
                        hoja,
                        fila,
                        columna)));
    }

    private static bool TieneIdentificacionFactura(
        IXLWorksheet hoja,
        int fila,
        int columnaFe,
        int columnaPrefijo,
        int columnaFactura)
    {
        return
            !string.IsNullOrWhiteSpace(
                ObtenerTexto(
                    hoja,
                    fila,
                    columnaFe))
            ||
            !string.IsNullOrWhiteSpace(
                ObtenerTexto(
                    hoja,
                    fila,
                    columnaPrefijo))
            ||
            !string.IsNullOrWhiteSpace(
                ObtenerTexto(
                    hoja,
                    fila,
                    columnaFactura));
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

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        int columna)
    {
        return hoja.Cell(fila, columna)
            .CachedValue
            .ToString()
            .Trim();
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

    private static ResultadoAnalisisImportacionDto
        CrearResultadoInvalido(
            string nombreArchivo,
            ResultadoInspeccionPlantillaDto inspeccion)
    {
        return new ResultadoAnalisisImportacionDto
        {
            NombreArchivo = nombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

            AniosDetectados =
                Array.Empty<int>(),

            TotalFilasAnalizadas = 0,
            FacturasDetectadas = 0,
            MovimientosDetectados = 0,
            CatalogosNoMapeados = 0,

            Inconsistencias =
                inspeccion.Inconsistencias
        };
    }

    private sealed record ResultadoFilas(
        int TotalFilas,
        int FacturasDetectadas,
        IReadOnlyCollection<int> AniosDetectados);
}