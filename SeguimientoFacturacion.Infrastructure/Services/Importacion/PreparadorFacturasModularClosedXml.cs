using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Domain.Constants;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Transforma una plantilla modular validada en facturas
/// preparadas en memoria, sin escribir en SQL Server.
/// </summary>
public sealed class
    PreparadorFacturasModularClosedXml :
    IPreparadorImportacionFacturacion
{
    private readonly
        LectorFacturasModularValidadoClosedXml
        _lectorValidado;

    /// <summary>
    /// Inicializa el preparador modular de facturas.
    /// </summary>
    public PreparadorFacturasModularClosedXml(
        LectorFacturasModularValidadoClosedXml
            lectorValidado)
    {
        ArgumentNullException.ThrowIfNull(
            lectorValidado);

        _lectorValidado = lectorValidado;
    }

    /// <inheritdoc />
    public async Task<ResultadoPreparacionImportacionDto>
        PrepararAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(
            solicitud.Contenido);

        cancellationToken.ThrowIfCancellationRequested();

        var contexto =
            await _lectorValidado
                .AnalizarConContextoAsync(
                    solicitud,
                    cancellationToken);

        if (!contexto.Analisis.EsValido)
        {
            throw new InvalidOperationException(
                "El archivo no puede prepararse porque " +
                $"contiene {contexto.Analisis.TotalErrores} " +
                "error(es) bloqueante(s).");
        }

        if (contexto.Catalogos is null)
        {
            throw new InvalidOperationException(
                "El archivo superó la validación, pero no " +
                "se encuentran disponibles los catálogos.");
        }

        await using var contenidoLocal =
            await CopiarContenidoAsync(
                solicitud.Contenido,
                cancellationToken);

        using var libro =
            new XLWorkbook(contenidoLocal);

        var hoja =
            libro.Worksheets.SingleOrDefault(
                elemento =>
                    string.Equals(
                        elemento.Name,
                        contexto.Inspeccion.NombreHojaDatos,
                        StringComparison.Ordinal));

        if (hoja is null)
        {
            throw new InvalidOperationException(
                "La hoja validada no existe en el archivo.");
        }

        var indicesCatalogos =
            CrearIndicesCatalogos(
                contexto.Catalogos);

        var facturas =
            PrepararFacturas(
                hoja,
                contexto.Inspeccion,
                indicesCatalogos,
                cancellationToken);

        return new ResultadoPreparacionImportacionDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            Facturas =
                facturas
        };
    }

    private static IReadOnlyCollection<
        FacturaPreparadaImportacionDto>
        PrepararFacturas(
            IXLWorksheet hoja,
            ResultadoInspeccionPlantillaDto inspeccion,
            IndicesCatalogos indicesCatalogos,
            CancellationToken cancellationToken)
    {
        var facturas =
            new List<FacturaPreparadaImportacionDto>();

        for (var fila = inspeccion.PrimeraFilaDatos;
             fila <= inspeccion.UltimaFilaUtilizada;
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
                    inspeccion.Columnas.Values))
            {
                continue;
            }

            facturas.Add(
                PrepararFactura(
                    hoja,
                    fila,
                    inspeccion.Columnas,
                    indicesCatalogos));
        }

        return facturas;
    }

    private static FacturaPreparadaImportacionDto
        PrepararFactura(
            IXLWorksheet hoja,
            int fila,
            IReadOnlyDictionary<string, int> columnas,
            IndicesCatalogos catalogos)
    {
        var estadoTexto =
            ObtenerTextoRequerido(
                hoja,
                fila,
                columnas,
                "ESTADO DE DTO");

        var estadoId =
            ResolverCatalogoId(
                estadoTexto,
                catalogos.Estados,
                "estado");

        var fechaRadicacion =
            estadoId == CodigosEstadoFactura.Anulada
                ? null
                : ObtenerFechaOpcional(
                    hoja,
                    fila,
                    columnas,
                    "FECHA DE RADICACION");

        return new FacturaPreparadaImportacionDto
        {
            HojaOrigen =
                hoja.Name,

            FilaOrigen =
                fila,

            IdentificadorFe =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "FE")),

            Prefijo =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "PREFIJO")),

            Numero =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "FACTURA")),

            FechaFactura =
                ObtenerFechaRequerida(
                    hoja,
                    fila,
                    columnas,
                    "FECHA FACTURA"),

            AseguradoraId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "ASEGURADORA"),
                    catalogos.Aseguradoras,
                    "aseguradora"),

            Valor =
                ObtenerDecimalRequerido(
                    hoja,
                    fila,
                    columnas,
                    "VALOR"),

            FechaRadicacion =
                fechaRadicacion,

            TipoDocumentoId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "TIPO DTO"),
                    catalogos.TiposDocumento,
                    "tipo de documento"),

            NumeroDocumento =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "NUMERO DTO")),

            NombreCompleto =
                NormalizarTexto(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "NOMBRE COMPLETO")),

            AtencionId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "ATENCION"),
                    catalogos.Atenciones,
                    "atención"),

            CostoId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "COSTO"),
                    catalogos.Costos,
                    "costo"),

            NumeroAdmision =
                NormalizarIdentificadorOpcional(
                    ObtenerTexto(
                        hoja,
                        fila,
                        columnas,
                        "NO ADMISION")),

            FechaAdmision =
                ObtenerFechaOpcional(
                    hoja,
                    fila,
                    columnas,
                    "FECHA ADMISION"),

            EstadoId =
                estadoId,

            FacturadorId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas,
                        "FACTURADOR"),
                    catalogos.Facturadores,
                    "facturador"),

            /*
             * Las notas, glosas y pagos se importarán
             * mediante sus propios archivos modulares.
             */
            Movimientos =
                Array.Empty<
                    MovimientoPreparadoImportacionDto>()
        };
    }

    private static IndicesCatalogos
        CrearIndicesCatalogos(
            CatalogosImportacionDto catalogos)
    {
        return new IndicesCatalogos(
            CrearIndice(
                catalogos.Aseguradoras),

            CrearIndice(
                catalogos.TiposDocumento),

            CrearIndice(
                catalogos.Atenciones),

            CrearIndice(
                catalogos.Costos),

            CrearIndice(
                catalogos.Estados,
                incluirIdentificadores: true),

            CrearIndice(
                catalogos.Facturadores));
    }

    private static IReadOnlyDictionary<string, int>
        CrearIndice(
            IEnumerable<ReferenciaCatalogoImportacionDto>
                elementos,
            bool incluirIdentificadores = false)
    {
        var indice =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (var elemento in elementos)
        {
            if (!string.IsNullOrWhiteSpace(
                    elemento.Valor))
            {
                indice.TryAdd(
                    NormalizadorEncabezadoImportacion
                        .Normalizar(elemento.Valor),
                    elemento.Id);
            }

            if (incluirIdentificadores)
            {
                indice.TryAdd(
                    elemento.Id.ToString(
                        CultureInfo.InvariantCulture),
                    elemento.Id);
            }
        }

        return indice;
    }

    private static int ResolverCatalogoId(
        string valor,
        IReadOnlyDictionary<string, int> indice,
        string nombreCatalogo)
    {
        var valorNormalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(valor);

        if (indice.TryGetValue(
                valorNormalizado,
                out var identificador))
        {
            return identificador;
        }

        throw new InvalidOperationException(
            "No fue posible resolver un valor del " +
            $"catálogo de {nombreCatalogo} después de " +
            "haber validado el archivo.");
    }

    private static string ObtenerTextoRequerido(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        var texto =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                nombreColumna);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            return texto;
        }

        throw new InvalidOperationException(
            $"La columna {nombreColumna} quedó vacía " +
            "después de haber validado el archivo.");
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        return hoja.Cell(
                fila,
                columnas[nombreColumna])
            .CachedValue
            .ToString()
            .Trim();
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

        if (string.IsNullOrWhiteSpace(
                celda.CachedValue
                    .ToString()
                    .Trim()))
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

    private static string?
        NormalizarIdentificadorOpcional(
            string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : NormalizarIdentificador(valor);
    }

    private static string NormalizarTexto(
        string valor)
    {
        return valor.Trim();
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

    private sealed record IndicesCatalogos(
        IReadOnlyDictionary<string, int> Aseguradoras,
        IReadOnlyDictionary<string, int> TiposDocumento,
        IReadOnlyDictionary<string, int> Atenciones,
        IReadOnlyDictionary<string, int> Costos,
        IReadOnlyDictionary<string, int> Estados,
        IReadOnlyDictionary<string, int> Facturadores);
}
