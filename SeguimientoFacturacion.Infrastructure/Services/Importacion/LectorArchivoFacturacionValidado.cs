using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Complementa el análisis estructural con validaciones
/// detalladas por fila y contra los catálogos normalizados.
/// </summary>
public sealed class LectorArchivoFacturacionValidado :
    ILectorArchivoFacturacion
{
    private const int FilaEncabezados = 1;
    private const int PrimeraFilaDatos = 3;

    private readonly LectorArchivoFacturacionClosedXml
        _lectorEstructural;

    private readonly IConsultaCatalogosImportacion
        _consultaCatalogos;

    /// <summary>
    /// Inicializa una nueva instancia del lector validado.
    /// </summary>
    public LectorArchivoFacturacionValidado(
        LectorArchivoFacturacionClosedXml lectorEstructural,
        IConsultaCatalogosImportacion consultaCatalogos)
    {
        ArgumentNullException.ThrowIfNull(
            lectorEstructural);

        ArgumentNullException.ThrowIfNull(
            consultaCatalogos);

        _lectorEstructural = lectorEstructural;
        _consultaCatalogos = consultaCatalogos;
    }

    /// <inheritdoc />
    public async Task<ResultadoAnalisisImportacionDto>
        AnalizarAsync(
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

        var solicitudEstructural =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo =
                    solicitud.NombreArchivo,

                Contenido = contenidoLocal
            };

        var resultadoEstructural =
            await _lectorEstructural.AnalizarAsync(
                solicitudEstructural,
                cancellationToken);

        if (!resultadoEstructural.EsValido)
        {
            return resultadoEstructural;
        }

        contenidoLocal.Position = 0;

        var catalogos =
            await _consultaCatalogos.ObtenerAsync(
                cancellationToken);

        using var libro =
            new XLWorkbook(contenidoLocal);

        var resultadoDetallado =
            AnalizarDetalle(
                libro,
                catalogos,
                cancellationToken);

        var inconsistenciasMovimientos =
        ValidadorMovimientosArchivoFacturacionClosedXml
        .Validar(
            libro,
            cancellationToken);

        return resultadoEstructural with
        {
            CatalogosNoMapeados =
                resultadoDetallado
                    .CatalogosNoMapeados,

            Inconsistencias =
                resultadoEstructural.Inconsistencias
               .Concat(
                    resultadoDetallado
                .Inconsistencias)
                .Concat(
                    inconsistenciasMovimientos)
                .ToArray()
        };
    }

    private static ResultadoDetalle AnalizarDetalle(
        XLWorkbook libro,
        CatalogosImportacionDto catalogos,
        CancellationToken cancellationToken)
    {
        var inconsistencias =
            new List<InconsistenciaImportacionDto>();

        var catalogosNoMapeados =
            new HashSet<string>(
                StringComparer.Ordinal);

        var facturasEncontradas =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var indicesCatalogos =
            CrearIndicesCatalogos(catalogos);

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

            var encabezados = ObtenerEncabezados(
                hoja,
                ultimaColumna);

            if (!EsHojaFacturacion(encabezados))
            {
                continue;
            }

            var columnas = ResolverColumnas(
                hoja,
                encabezados,
                ultimaColumna,
                inconsistencias);

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

                ValidarFila(
                    hoja,
                    fila,
                    columnas,
                    indicesCatalogos,
                    facturasEncontradas,
                    catalogosNoMapeados,
                    inconsistencias);
            }
        }

        return new ResultadoDetalle(
            catalogosNoMapeados.Count,
            inconsistencias);
    }

    private static void ValidarFila(
        IXLWorksheet hoja,
        int fila,
        ColumnasFactura columnas,
        IndicesCatalogos catalogos,
        ISet<string> facturasEncontradas,
        ISet<string> catalogosNoMapeados,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var fe = ObtenerTexto(
            hoja,
            fila,
            columnas.Fe);

        var prefijo = ObtenerTexto(
            hoja,
            fila,
            columnas.Prefijo);

        var numeroFactura = ObtenerTexto(
            hoja,
            fila,
            columnas.Factura);

        var aseguradora = ObtenerTexto(
            hoja,
            fila,
            columnas.Aseguradora);

        var tipoDocumento = ObtenerTexto(
            hoja,
            fila,
            columnas.TipoDocumento);

        var numeroDocumento = ObtenerTexto(
            hoja,
            fila,
            columnas.NumeroDocumento);

        var nombreCompleto = ObtenerTexto(
            hoja,
            fila,
            columnas.NombreCompleto);

        var atencion = ObtenerTexto(
            hoja,
            fila,
            columnas.Atencion);

        var costo = ObtenerTexto(
            hoja,
            fila,
            columnas.Costo);

        var estado = ObtenerTexto(
            hoja,
            fila,
            columnas.Estado);

        var facturador = ObtenerTexto(
            hoja,
            fila,
            columnas.Facturador);

        ValidarTextoRequerido(
            fe,
            fila,
            columnas.Fe,
            "FE",
            "FE_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            prefijo,
            fila,
            columnas.Prefijo,
            "PREFIJO",
            "PREFIJO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            numeroFactura,
            fila,
            columnas.Factura,
            "FACTURA",
            "FACTURA_REQUERIDA",
            inconsistencias);

        ValidarTextoRequerido(
            aseguradora,
            fila,
            columnas.Aseguradora,
            "ASEGURADORA",
            "ASEGURADORA_REQUERIDA",
            inconsistencias);

        ValidarTextoRequerido(
            tipoDocumento,
            fila,
            columnas.TipoDocumento,
            "TIPO DTO",
            "TIPO_DOCUMENTO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            numeroDocumento,
            fila,
            columnas.NumeroDocumento,
            "NÚMERO DTO",
            "NUMERO_DOCUMENTO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            nombreCompleto,
            fila,
            columnas.NombreCompleto,
            "NOMBRE COMPLETO",
            "NOMBRE_COMPLETO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            atencion,
            fila,
            columnas.Atencion,
            "ATENCIÓN",
            "ATENCION_REQUERIDA",
            inconsistencias);

        ValidarTextoRequerido(
            costo,
            fila,
            columnas.Costo,
            "COSTO",
            "COSTO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            estado,
            fila,
            columnas.Estado,
            "ESTADO DE DTO",
            "ESTADO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            facturador,
            fila,
            columnas.Facturador,
            "FACTURADOR",
            "FACTURADOR_REQUERIDO",
            inconsistencias);

        if (!string.IsNullOrWhiteSpace(fe))
        {
            var feNormalizado =
                fe.Trim().ToUpperInvariant();

            if (!facturasEncontradas.Add(
                    feNormalizado))
            {
                AgregarError(
                    inconsistencias,
                    fila,
                    "FE",
                    "FACTURA_DUPLICADA",
                    "El identificador FE ya apareció " +
                    "anteriormente en el archivo.");
            }
        }

        var fechaFacturaValida =
            ValidarFechaFactura(
                hoja,
                fila,
                columnas.FechaFactura,
                inconsistencias,
                out var fechaFactura);

        ValidarValorFactura(
            hoja,
            fila,
            columnas.Valor,
            inconsistencias);

        ValidarFechaRadicacion(
            hoja,
            fila,
            columnas.FechaRadicacion,
            fechaFacturaValida,
            fechaFactura,
            inconsistencias);

        ValidarFechaAdmision(
            hoja,
            fila,
            columnas.FechaAdmision,
            fechaFacturaValida,
            fechaFactura,
            inconsistencias);

        ValidarCatalogo(
            aseguradora,
            fila,
            "ASEGURADORA",
            "CATALOGO_ASEGURADORA_NO_MAPEADO",
            catalogos.Aseguradoras,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            tipoDocumento,
            fila,
            "TIPO DTO",
            "CATALOGO_TIPO_DOCUMENTO_NO_MAPEADO",
            catalogos.TiposDocumento,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            atencion,
            fila,
            "ATENCIÓN",
            "CATALOGO_ATENCION_NO_MAPEADO",
            catalogos.Atenciones,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            costo,
            fila,
            "COSTO",
            "CATALOGO_COSTO_NO_MAPEADO",
            catalogos.Costos,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            estado,
            fila,
            "ESTADO DE DTO",
            "CATALOGO_ESTADO_NO_MAPEADO",
            catalogos.Estados,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            facturador,
            fila,
            "FACTURADOR",
            "CATALOGO_FACTURADOR_NO_MAPEADO",
            catalogos.Facturadores,
            catalogosNoMapeados,
            inconsistencias);
    }

    private static ColumnasFactura ResolverColumnas(
        IXLWorksheet hoja,
        IReadOnlyDictionary<string, int> encabezados,
        int ultimaColumna,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var fe = RequerirColumna(
            encabezados,
            "FE",
            inconsistencias,
            "FE");

        var prefijo = RequerirColumna(
            encabezados,
            "PREFIJO",
            inconsistencias,
            "PREFIJO");

        var factura = RequerirColumna(
            encabezados,
            "FACTURA",
            inconsistencias,
            "FACTURA");

        var fechaFactura = RequerirColumna(
            encabezados,
            "FECHA FRA",
            inconsistencias,
            "FECHAFRA");

        var valor = RequerirColumna(
            encabezados,
            "VALOR",
            inconsistencias,
            "VALOR");

        var fechaRadicacion = RequerirColumna(
            encabezados,
            "FECHA DE RADICACIÓN",
            inconsistencias,
            "FECHADERADICACION");

        var tipoDocumento = RequerirColumna(
            encabezados,
            "TIPO DTO",
            inconsistencias,
            "TIPODTO");

        var numeroDocumento = RequerirColumna(
            encabezados,
            "NÚMERO DTO",
            inconsistencias,
            "NUMERODTO");

        var nombreCompleto = RequerirColumna(
            encabezados,
            "NOMBRE COMPLETO",
            inconsistencias,
            "NOMBRECOMPLETO");

        var atencion = RequerirColumna(
            encabezados,
            "ATENCIÓN",
            inconsistencias,
            "ATENCION");

        var costo = RequerirColumna(
            encabezados,
            "COSTO",
            inconsistencias,
            "COSTO");

        var numeroAdmision = RequerirColumna(
            encabezados,
            "No ADMISIÓN",
            inconsistencias,
            "NOADMISION");

        var fechaAdmision = RequerirColumna(
            encabezados,
            "FECHA ADMISIÓN",
            inconsistencias,
            "FECHAADMISION");

        var estado = RequerirColumna(
            encabezados,
            "ESTADO DE DTO",
            inconsistencias,
            "ESTADODEDTO");

        var aseguradora = BuscarColumna(
            encabezados,
            "ASEGURADORA");

        /*
         * Compatibilidad controlada con Seguimiento 2024:
         * la columna E contiene aseguradora, pero el
         * encabezado se encuentra vacío.
         */
        if (!aseguradora.HasValue &&
            ultimaColumna >= 6 &&
            BuscarColumna(encabezados, "VALOR") == 6)
        {
            aseguradora = 5;

            AgregarAdvertencia(
                inconsistencias,
                FilaEncabezados,
                "ASEGURADORA",
                "ENCABEZADO_ASEGURADORA_INFERIDO",
                "El encabezado de aseguradora está vacío. " +
                "Se utilizó temporalmente la columna E.");
        }
        else if (!aseguradora.HasValue)
        {
            AgregarError(
                inconsistencias,
                FilaEncabezados,
                "ASEGURADORA",
                "ENCABEZADO_REQUERIDO_AUSENTE",
                "No se encontró el encabezado obligatorio.");
        }

        var facturador = BuscarColumna(
            encabezados,
            "FACTURADOR");

        if (!facturador.HasValue)
        {
            facturador = BuscarColumna(
                encabezados,
                "FACTURARDOR");

            if (facturador.HasValue)
            {
                AgregarAdvertencia(
                    inconsistencias,
                    FilaEncabezados,
                    "FACTURADOR",
                    "ENCABEZADO_FACTURADOR_NO_ESTANDAR",
                    "El encabezado FACTURARDOR debe " +
                    "corregirse por FACTURADOR.");
            }
            else
            {
                AgregarError(
                    inconsistencias,
                    FilaEncabezados,
                    "FACTURADOR",
                    "ENCABEZADO_REQUERIDO_AUSENTE",
                    "No se encontró el encabezado obligatorio.");
            }
        }

        return new ColumnasFactura(
            fe,
            prefijo,
            factura,
            fechaFactura,
            aseguradora,
            valor,
            fechaRadicacion,
            tipoDocumento,
            numeroDocumento,
            nombreCompleto,
            atencion,
            costo,
            numeroAdmision,
            fechaAdmision,
            estado,
            facturador);
    }

    private static int? RequerirColumna(
        IReadOnlyDictionary<string, int> encabezados,
        string nombreVisible,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        params string[] aliases)
    {
        var columna = BuscarColumna(
            encabezados,
            aliases);

        if (columna.HasValue)
        {
            return columna;
        }

        AgregarError(
            inconsistencias,
            FilaEncabezados,
            nombreVisible,
            "ENCABEZADO_REQUERIDO_AUSENTE",
            "No se encontró el encabezado obligatorio.");

        return null;
    }

    private static int? BuscarColumna(
        IReadOnlyDictionary<string, int> encabezados,
        params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (encabezados.TryGetValue(
                    alias,
                    out var columna))
            {
                return columna;
            }
        }

        return null;
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
            var texto = NormalizarTexto(
                ObtenerTextoCelda(
                    hoja.Cell(
                        FilaEncabezados,
                        columna)));

            if (!string.IsNullOrWhiteSpace(texto))
            {
                encabezados.TryAdd(
                    texto,
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

    private static void ValidarTextoRequerido(
        string texto,
        int fila,
        int? columna,
        string nombreColumna,
        string codigo,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!columna.HasValue ||
            !string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        AgregarError(
            inconsistencias,
            fila,
            nombreColumna,
            codigo,
            "La fila no contiene el dato obligatorio.");
    }

    private static bool ValidarFechaFactura(
        IXLWorksheet hoja,
        int fila,
        int? columna,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        out DateOnly fecha)
    {
        fecha = default;

        if (!columna.HasValue)
        {
            return false;
        }

        var celda = hoja.Cell(
            fila,
            columna.Value);

        if (string.IsNullOrWhiteSpace(
                ObtenerTextoCelda(celda)))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA FRA",
                "FECHA_FACTURA_REQUERIDA",
                "La fila no contiene la fecha de factura.");

            return false;
        }

        if (IntentarObtenerFecha(
                celda,
                out fecha))
        {
            return true;
        }

        AgregarError(
            inconsistencias,
            fila,
            "FECHA FRA",
            "FECHA_FACTURA_INVALIDA",
            "El valor no corresponde a una fecha válida.");

        return false;
    }

    private static void ValidarValorFactura(
        IXLWorksheet hoja,
        int fila,
        int? columna,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!columna.HasValue)
        {
            return;
        }

        var celda = hoja.Cell(
            fila,
            columna.Value);

        if (string.IsNullOrWhiteSpace(
                ObtenerTextoCelda(celda)))
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR",
                "VALOR_FACTURA_REQUERIDO",
                "La fila no contiene el valor de la factura.");

            return;
        }

        if (!IntentarObtenerDecimal(
                celda,
                out var valor))
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR",
                "VALOR_FACTURA_INVALIDO",
                "El valor de la factura no es numérico.");

            return;
        }

        if (valor <= decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR",
                "VALOR_FACTURA_NO_POSITIVO",
                "El valor de la factura debe ser mayor que cero.");
        }
    }

    private static void ValidarFechaRadicacion(
        IXLWorksheet hoja,
        int fila,
        int? columna,
        bool fechaFacturaValida,
        DateOnly fechaFactura,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!columna.HasValue)
        {
            return;
        }

        var celda = hoja.Cell(
            fila,
            columna.Value);

        if (string.IsNullOrWhiteSpace(
                ObtenerTextoCelda(celda)))
        {
            return;
        }

        if (!IntentarObtenerFecha(
                celda,
                out var fechaRadicacion))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE RADICACIÓN",
                "FECHA_RADICACION_INVALIDA",
                "El valor no corresponde a una fecha válida.");

            return;
        }

        if (fechaFacturaValida &&
            fechaRadicacion < fechaFactura)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE RADICACIÓN",
                "FECHA_RADICACION_ANTERIOR",
                "La fecha de radicación no puede ser " +
                "anterior a la fecha de factura.");
        }
    }

    private static void ValidarFechaAdmision(
        IXLWorksheet hoja,
        int fila,
        int? columna,
        bool fechaFacturaValida,
        DateOnly fechaFactura,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!columna.HasValue)
        {
            return;
        }

        var celda = hoja.Cell(
            fila,
            columna.Value);

        if (string.IsNullOrWhiteSpace(
                ObtenerTextoCelda(celda)))
        {
            return;
        }

        if (!IntentarObtenerFecha(
                celda,
                out var fechaAdmision))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA ADMISIÓN",
                "FECHA_ADMISION_INVALIDA",
                "El valor no corresponde a una fecha válida.");

            return;
        }

        if (fechaFacturaValida &&
            fechaAdmision > fechaFactura)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA ADMISIÓN",
                "FECHA_ADMISION_POSTERIOR",
                "La fecha de admisión no puede ser " +
                "posterior a la fecha de factura.");
        }
    }

    private static bool IntentarObtenerFecha(
        IXLCell celda,
        out DateOnly fecha)
    {
        if (celda.TryGetValue<DateTime>(
                out var fechaHora))
        {
            fecha = DateOnly.FromDateTime(
                fechaHora);

            return true;
        }

        var texto = ObtenerTextoCelda(celda);

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

        var texto = ObtenerTextoCelda(celda);

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

    private static void ValidarCatalogo(
        string valor,
        int fila,
        string columna,
        string codigo,
        IReadOnlySet<string> valoresValidos,
        ISet<string> catalogosNoMapeados,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return;
        }

        var valorNormalizado =
            NormalizarTexto(valor);

        if (valoresValidos.Contains(
                valorNormalizado))
        {
            return;
        }

        var clave =
            $"{codigo}:{valorNormalizado}";

        /*
         * Se registra solamente la primera aparición de
         * cada valor de catálogo sin correspondencia.
         */
        if (!catalogosNoMapeados.Add(clave))
        {
            return;
        }

        AgregarError(
            inconsistencias,
            fila,
            columna,
            codigo,
            "El valor utilizado no existe en el " +
            "catálogo normalizado.");
    }

    private static IndicesCatalogos CrearIndicesCatalogos(
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

    private static IReadOnlySet<string> CrearIndice(
        IEnumerable<ReferenciaCatalogoImportacionDto>
            elementos,
        bool incluirIdentificadores = false)
    {
        var indice =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var elemento in elementos)
        {
            if (!string.IsNullOrWhiteSpace(
                    elemento.Valor))
            {
                indice.Add(
                    NormalizarTexto(
                        elemento.Valor));
            }

            if (incluirIdentificadores)
            {
                indice.Add(
                    elemento.Id.ToString(
                        CultureInfo.InvariantCulture));
            }
        }

        return indice;
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        int? columna)
    {
        if (!columna.HasValue)
        {
            return string.Empty;
        }

        return ObtenerTextoCelda(
            hoja.Cell(
                fila,
                columna.Value));
    }

    private static string ObtenerTextoCelda(
        IXLCell celda)
    {
        return celda.CachedValue
            .ToString()
            .Trim();
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

        foreach (var caracter
                 in textoDescompuesto)
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
                    char.ToUpperInvariant(
                        caracter));
            }
        }

        return resultado.ToString()
            .Normalize(
                NormalizationForm.FormC);
    }

    private static void AgregarError(
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        int? fila,
        string columna,
        string codigo,
        string mensaje)
    {
        inconsistencias.Add(
            new InconsistenciaImportacionDto
            {
                Fila = fila,
                Columna = columna,
                Codigo = codigo,
                Mensaje = mensaje,
                Severidad =
                    SeveridadInconsistenciaImportacion.Error
            });
    }

    private static void AgregarAdvertencia(
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        int? fila,
        string columna,
        string codigo,
        string mensaje)
    {
        inconsistencias.Add(
            new InconsistenciaImportacionDto
            {
                Fila = fila,
                Columna = columna,
                Codigo = codigo,
                Mensaje = mensaje,
                Severidad =
                    SeveridadInconsistenciaImportacion
                        .Advertencia
            });
    }

    private sealed record ColumnasFactura(
        int? Fe,
        int? Prefijo,
        int? Factura,
        int? FechaFactura,
        int? Aseguradora,
        int? Valor,
        int? FechaRadicacion,
        int? TipoDocumento,
        int? NumeroDocumento,
        int? NombreCompleto,
        int? Atencion,
        int? Costo,
        int? NumeroAdmision,
        int? FechaAdmision,
        int? Estado,
        int? Facturador);

    private sealed record IndicesCatalogos(
        IReadOnlySet<string> Aseguradoras,
        IReadOnlySet<string> TiposDocumento,
        IReadOnlySet<string> Atenciones,
        IReadOnlySet<string> Costos,
        IReadOnlySet<string> Estados,
        IReadOnlySet<string> Facturadores);

    private sealed record ResultadoDetalle(
        int CatalogosNoMapeados,
        IReadOnlyCollection<
            InconsistenciaImportacionDto> Inconsistencias);
}