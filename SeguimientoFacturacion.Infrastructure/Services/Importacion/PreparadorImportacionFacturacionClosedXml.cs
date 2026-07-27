using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Transforma un archivo previamente validado en facturas
/// preparadas en memoria, sin escribir en SQL Server.
/// </summary>
public sealed class PreparadorImportacionFacturacionClosedXml :
    IPreparadorImportacionFacturacion
{
    private const int FilaEncabezados = 1;
    private const int PrimeraFilaDatos = 3;
    private const int EstadoAnuladoId = 5;

    private readonly ILectorArchivoFacturacion
        _lectorArchivoFacturacion;

    private readonly IConsultaCatalogosImportacion
        _consultaCatalogos;

    /// <summary>
    /// Inicializa el preparador de importación.
    /// </summary>
    public PreparadorImportacionFacturacionClosedXml(
        ILectorArchivoFacturacion lectorArchivoFacturacion,
        IConsultaCatalogosImportacion consultaCatalogos)
    {
        ArgumentNullException.ThrowIfNull(
            lectorArchivoFacturacion);

        ArgumentNullException.ThrowIfNull(
            consultaCatalogos);

        _lectorArchivoFacturacion =
            lectorArchivoFacturacion;

        _consultaCatalogos = consultaCatalogos;
    }

    /// <inheritdoc />
    public async Task<ResultadoPreparacionImportacionDto>
        PrepararAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        cancellationToken.ThrowIfCancellationRequested();

        await using var contenidoLocal =
            new MemoryStream();

        if (solicitud.Contenido.CanSeek)
        {
            solicitud.Contenido.Position = 0;
        }

        await solicitud.Contenido.CopyToAsync(
            contenidoLocal,
            cancellationToken);

        contenidoLocal.Position = 0;

        var solicitudAnalisis =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = solicitud.NombreArchivo,
                Contenido = contenidoLocal
            };

        var analisis =
            await _lectorArchivoFacturacion.AnalizarAsync(
                solicitudAnalisis,
                cancellationToken);

        if (!analisis.EsValido)
        {
            throw new InvalidOperationException(
                $"El archivo no puede prepararse porque contiene " +
                $"{analisis.TotalErrores} error(es) bloqueante(s).");
        }

        contenidoLocal.Position = 0;

        var catalogos =
            await _consultaCatalogos.ObtenerAsync(
                cancellationToken);

        var indicesCatalogos =
            CrearIndicesCatalogos(catalogos);

        using var libro =
            new XLWorkbook(contenidoLocal);

        var facturas =
            PrepararFacturas(
                libro,
                indicesCatalogos,
                cancellationToken);

        return new ResultadoPreparacionImportacionDto
        {
            NombreArchivo = solicitud.NombreArchivo,
            Facturas = facturas
        };
    }

    private static IReadOnlyCollection<
        FacturaPreparadaImportacionDto> PrepararFacturas(
            XLWorkbook libro,
            IndicesCatalogos indicesCatalogos,
            CancellationToken cancellationToken)
    {
        var facturas =
            new List<FacturaPreparadaImportacionDto>();

        foreach (var hoja in libro.Worksheets)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var ultimaColumna =
                hoja.LastColumnUsed()?.ColumnNumber()
                ?? 0;

            if (ultimaColumna == 0)
            {
                continue;
            }

            var encabezados =
                ObtenerEncabezados(
                    hoja,
                    ultimaColumna);

            if (!EsHojaFacturacion(encabezados))
            {
                continue;
            }

            var columnas =
                ResolverColumnas(
                    encabezados,
                    ultimaColumna);

            var esquemaMovimientos =
                ExtractorMovimientosFacturacionClosedXml
                    .Detectar(
                        hoja,
                        ultimaColumna);

            var ultimaFila =
                hoja.LastRowUsed()?.RowNumber()
                ?? 0;

            for (var fila = PrimeraFilaDatos;
                 fila <= ultimaFila;
                 fila++)
            {
                if (fila % 256 == 0)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();
                }

                if (!EsFilaFactura(
                        hoja,
                        fila,
                        columnas))
                {
                    continue;
                }

                facturas.Add(
                    PrepararFactura(
                        hoja,
                        fila,
                        columnas,
                        indicesCatalogos,
                        esquemaMovimientos));
            }
        }

        return facturas;
    }

    private static FacturaPreparadaImportacionDto
        PrepararFactura(
            IXLWorksheet hoja,
            int fila,
            ColumnasFactura columnas,
            IndicesCatalogos catalogos,
            ExtractorMovimientosFacturacionClosedXml
                .EsquemaMovimientos esquemaMovimientos)
    {
        var estadoTexto =
            ObtenerTextoRequerido(
                hoja,
                fila,
                columnas.Estado,
                "ESTADO DE DTO");

        var estadoId =
            ResolverCatalogoId(
                estadoTexto,
                catalogos.Estados,
                "estado");

        var fechaRadicacion =
            estadoId == EstadoAnuladoId
                ? null
                : ObtenerFechaOpcional(
                    hoja,
                    fila,
                    columnas.FechaRadicacion,
                    "FECHA DE RADICACIÓN");

        return new FacturaPreparadaImportacionDto
        {
            HojaOrigen = hoja.Name,
            FilaOrigen = fila,

            IdentificadorFe =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Fe,
                        "FE")),

            Prefijo =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Prefijo,
                        "PREFIJO")),

            Numero =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Factura,
                        "FACTURA")),

            FechaFactura =
                ObtenerFechaRequerida(
                    hoja,
                    fila,
                    columnas.FechaFactura,
                    "FECHA FRA"),

            AseguradoraId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Aseguradora,
                        "ASEGURADORA"),
                    catalogos.Aseguradoras,
                    "aseguradora"),

            Valor =
                ObtenerDecimalRequerido(
                    hoja,
                    fila,
                    columnas.Valor,
                    "VALOR"),

            FechaRadicacion = fechaRadicacion,

            TipoDocumentoId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.TipoDocumento,
                        "TIPO DTO"),
                    catalogos.TiposDocumento,
                    "tipo de documento"),

            NumeroDocumento =
                NormalizarIdentificador(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.NumeroDocumento,
                        "NÚMERO DTO")),

            NombreCompleto =
                ObtenerTextoRequerido(
                    hoja,
                    fila,
                    columnas.NombreCompleto,
                    "NOMBRE COMPLETO")
                    .Trim(),

            AtencionId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Atencion,
                        "ATENCIÓN"),
                    catalogos.Atenciones,
                    "atención"),

            CostoId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Costo,
                        "COSTO"),
                    catalogos.Costos,
                    "costo"),

            NumeroAdmision =
                NormalizarIdentificadorOpcional(
                    ObtenerTexto(
                        hoja,
                        fila,
                        columnas.NumeroAdmision)),

            FechaAdmision =
                ObtenerFechaOpcional(
                    hoja,
                    fila,
                    columnas.FechaAdmision,
                    "FECHA ADMISIÓN"),

            EstadoId = estadoId,

            FacturadorId =
                ResolverCatalogoId(
                    ObtenerTextoRequerido(
                        hoja,
                        fila,
                        columnas.Facturador,
                        "FACTURADOR"),
                    catalogos.Facturadores,
                    "facturador"),

            Movimientos =
                ExtractorMovimientosFacturacionClosedXml
             .Extraer(
                hoja,
                fila,
                esquemaMovimientos)
        };
    }

    private static ColumnasFactura ResolverColumnas(
        IReadOnlyDictionary<string, int> encabezados,
        int ultimaColumna)
    {
        var aseguradora =
            BuscarColumna(
                encabezados,
                "ASEGURADORA");

        /*
         * Compatibilidad controlada con el archivo 2024:
         * el encabezado de la columna E se encuentra vacío,
         * mientras VALOR está ubicado en la columna F.
         */
        if (!aseguradora.HasValue &&
            ultimaColumna >= 6 &&
            BuscarColumna(
                encabezados,
                "VALOR") == 6)
        {
            aseguradora = 5;
        }

        var facturador =
            BuscarColumna(
                encabezados,
                "FACTURADOR",
                "FACTURARDOR");

        return new ColumnasFactura(
            RequerirColumna(
                encabezados,
                "FE"),

            RequerirColumna(
                encabezados,
                "PREFIJO"),

            RequerirColumna(
                encabezados,
                "FACTURA"),

            RequerirColumna(
                encabezados,
                "FECHAFRA"),

            aseguradora ??
            throw CrearExcepcionColumna(
                "ASEGURADORA"),

            RequerirColumna(
                encabezados,
                "VALOR"),

            RequerirColumna(
                encabezados,
                "FECHADERADICACION"),

            RequerirColumna(
                encabezados,
                "TIPODTO"),

            RequerirColumna(
                encabezados,
                "NUMERODTO"),

            RequerirColumna(
                encabezados,
                "NOMBRECOMPLETO"),

            RequerirColumna(
                encabezados,
                "ATENCION"),

            RequerirColumna(
                encabezados,
                "COSTO"),

            RequerirColumna(
                encabezados,
                "NOADMISION"),

            RequerirColumna(
                encabezados,
                "FECHAADMISION"),

            RequerirColumna(
                encabezados,
                "ESTADODEDTO"),

            facturador ??
            throw CrearExcepcionColumna(
                "FACTURADOR"));
    }

    private static IReadOnlyDictionary<string, int>
        ObtenerEncabezados(
            IXLWorksheet hoja,
            int ultimaColumna)
    {
        var encabezados =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        for (var columna = 1;
             columna <= ultimaColumna;
             columna++)
        {
            var encabezado =
                NormalizarTexto(
                    ObtenerTextoCelda(
                        hoja.Cell(
                            FilaEncabezados,
                            columna)));

            if (!string.IsNullOrWhiteSpace(
                    encabezado))
            {
                encabezados.TryAdd(
                    encabezado,
                    columna);
            }
        }

        return encabezados;
    }

    private static bool EsHojaFacturacion(
        IReadOnlyDictionary<string, int> encabezados)
    {
        return encabezados.ContainsKey("FE") &&
               encabezados.ContainsKey("PREFIJO") &&
               encabezados.ContainsKey("FACTURA") &&
               encabezados.ContainsKey("VALOR");
    }

    private static bool EsFilaFactura(
        IXLWorksheet hoja,
        int fila,
        ColumnasFactura columnas)
    {
        return !string.IsNullOrWhiteSpace(
                   ObtenerTexto(
                       hoja,
                       fila,
                       columnas.Fe)) ||
               !string.IsNullOrWhiteSpace(
                   ObtenerTexto(
                       hoja,
                       fila,
                       columnas.Prefijo)) ||
               !string.IsNullOrWhiteSpace(
                   ObtenerTexto(
                       hoja,
                       fila,
                       columnas.Factura));
    }

    private static int RequerirColumna(
        IReadOnlyDictionary<string, int> encabezados,
        params string[] nombres)
    {
        return BuscarColumna(
                   encabezados,
                   nombres)
               ?? throw CrearExcepcionColumna(
                   nombres[0]);
    }

    private static int? BuscarColumna(
        IReadOnlyDictionary<string, int> encabezados,
        params string[] nombres)
    {
        foreach (var nombre in nombres)
        {
            if (encabezados.TryGetValue(
                    nombre,
                    out var columna))
            {
                return columna;
            }
        }

        return null;
    }

    private static InvalidOperationException
        CrearExcepcionColumna(string columna)
    {
        return new InvalidOperationException(
            $"No se encontró la columna requerida " +
            $"'{columna}' después de validar el archivo.");
    }

    private static string ObtenerTextoRequerido(
        IXLWorksheet hoja,
        int fila,
        int columna,
        string nombreColumna)
    {
        var texto =
            ObtenerTexto(
                hoja,
                fila,
                columna);

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new InvalidOperationException(
                $"La fila {fila} no contiene el dato " +
                $"requerido '{nombreColumna}'.");
        }

        return texto;
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        int columna)
    {
        return ObtenerTextoCelda(
            hoja.Cell(fila, columna));
    }

    private static string ObtenerTextoCelda(
        IXLCell celda)
    {
        /*
         * Se utilizan los valores almacenados en caché
         * para no recalcular fórmulas con vínculos externos.
         */
        return celda.CachedValue
            .ToString()
            .Trim();
    }

    private static DateOnly ObtenerFechaRequerida(
        IXLWorksheet hoja,
        int fila,
        int columna,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(fila, columna);

        if (IntentarObtenerFecha(
                celda,
                out var fecha))
        {
            return fecha;
        }

        throw new InvalidOperationException(
            $"La fila {fila} contiene una fecha inválida " +
            $"en '{nombreColumna}'.");
    }

    private static DateOnly? ObtenerFechaOpcional(
        IXLWorksheet hoja,
        int fila,
        int columna,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(fila, columna);

        if (string.IsNullOrWhiteSpace(
                ObtenerTextoCelda(celda)))
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
            $"La fila {fila} contiene una fecha inválida " +
            $"en '{nombreColumna}'.");
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
            ObtenerTextoCelda(celda);

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

    private static decimal ObtenerDecimalRequerido(
        IXLWorksheet hoja,
        int fila,
        int columna,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(fila, columna);

        if (celda.TryGetValue<decimal>(
                out var valor))
        {
            return valor;
        }

        var texto =
            ObtenerTextoCelda(celda);

        if (decimal.TryParse(
                texto,
                NumberStyles.Number |
                NumberStyles.AllowCurrencySymbol,
                CultureInfo.GetCultureInfo("es-CO"),
                out valor))
        {
            return valor;
        }

        if (decimal.TryParse(
                texto,
                NumberStyles.Number |
                NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out valor))
        {
            return valor;
        }

        throw new InvalidOperationException(
            $"La fila {fila} contiene un valor inválido " +
            $"en '{nombreColumna}'.");
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
            var valorNormalizado =
                NormalizarTexto(elemento.Valor);

            if (!string.IsNullOrWhiteSpace(
                    valorNormalizado))
            {
                indice.TryAdd(
                    valorNormalizado,
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
            NormalizarTexto(valor);

        if (indice.TryGetValue(
                valorNormalizado,
                out var identificador))
        {
            return identificador;
        }

        throw new InvalidOperationException(
            $"El valor de {nombreCatalogo} no existe " +
            "en el catálogo normalizado.");
    }

    private static string NormalizarIdentificador(
        string texto)
    {
        return texto.Trim().ToUpperInvariant();
    }

    private static string?
        NormalizarIdentificadorOpcional(
            string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? null
            : NormalizarIdentificador(texto);
    }

    private static string NormalizarTexto(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var textoDescompuesto =
            texto.Trim()
                .Normalize(
                    NormalizationForm.FormD);

        var resultado =
            new StringBuilder(
                textoDescompuesto.Length);

        foreach (var caracter in textoDescompuesto)
        {
            var categoria =
                CharUnicodeInfo
                    .GetUnicodeCategory(caracter);

            if (categoria ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                resultado.Append(
                    char.ToUpperInvariant(caracter));
            }
        }

        return resultado.ToString()
            .Normalize(
                NormalizationForm.FormC);
    }

    private sealed record ColumnasFactura(
        int Fe,
        int Prefijo,
        int Factura,
        int FechaFactura,
        int Aseguradora,
        int Valor,
        int FechaRadicacion,
        int TipoDocumento,
        int NumeroDocumento,
        int NombreCompleto,
        int Atencion,
        int Costo,
        int NumeroAdmision,
        int FechaAdmision,
        int Estado,
        int Facturador);

    private sealed record IndicesCatalogos(
        IReadOnlyDictionary<string, int> Aseguradoras,
        IReadOnlyDictionary<string, int> TiposDocumento,
        IReadOnlyDictionary<string, int> Atenciones,
        IReadOnlyDictionary<string, int> Costos,
        IReadOnlyDictionary<string, int> Estados,
        IReadOnlyDictionary<string, int> Facturadores);
}