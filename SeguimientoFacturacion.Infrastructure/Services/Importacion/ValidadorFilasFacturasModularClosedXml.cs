using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Valida detalladamente las filas de una plantilla
/// modular de facturas mediante ClosedXML.
/// </summary>
public sealed class
    ValidadorFilasFacturasModularClosedXml :
    IValidadorFilasFacturasModular
{
    /// <inheritdoc />
    public async Task<ResultadoValidacionFilasFacturasDto>
        ValidarAsync(
            Stream contenido,
            ResultadoInspeccionPlantillaDto inspeccion,
            CatalogosImportacionDto catalogos,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        ArgumentNullException.ThrowIfNull(inspeccion);
        ArgumentNullException.ThrowIfNull(catalogos);

        cancellationToken.ThrowIfCancellationRequested();

        ValidarInspeccion(inspeccion);

        await using var copia =
            await CopiarContenidoAsync(
                contenido,
                cancellationToken);

        using var libro = new XLWorkbook(copia);

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
                "La hoja aprobada durante la inspección " +
                "estructural no existe en el archivo.");
        }

        return ValidarFilas(
            hoja,
            inspeccion,
            catalogos,
            cancellationToken);
    }

    private static ResultadoValidacionFilasFacturasDto
        ValidarFilas(
            IXLWorksheet hoja,
            ResultadoInspeccionPlantillaDto inspeccion,
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

        var aniosDetectados =
            new SortedSet<int>();

        var indicesCatalogos =
            CrearIndicesCatalogos(catalogos);

        var totalFilas = 0;
        var facturasDetectadas = 0;

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

            totalFilas++;

            if (TieneIdentificacionFactura(
                    hoja,
                    fila,
                    inspeccion.Columnas))
            {
                facturasDetectadas++;
            }

            ValidarFila(
                hoja,
                fila,
                inspeccion.Columnas,
                indicesCatalogos,
                facturasEncontradas,
                catalogosNoMapeados,
                aniosDetectados,
                inconsistencias);
        }

        return new ResultadoValidacionFilasFacturasDto
        {
            TotalFilasAnalizadas = totalFilas,
            FacturasDetectadas = facturasDetectadas,
            AniosDetectados = aniosDetectados.ToArray(),

            CatalogosNoMapeados =
                catalogosNoMapeados.Count,

            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static void ValidarFila(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        IndicesCatalogos indicesCatalogos,
        ISet<string> facturasEncontradas,
        ISet<string> catalogosNoMapeados,
        ISet<int> aniosDetectados,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var fe =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "FE");

        var prefijo =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "PREFIJO");

        var numeroFactura =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "FACTURA");

        var aseguradora =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "ASEGURADORA");

        var tipoDocumento =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "TIPO DTO");

        var numeroDocumento =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "NUMERO DTO");

        var nombreCompleto =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "NOMBRE COMPLETO");

        var atencion =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "ATENCION");

        var costo =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "COSTO");

        var numeroAdmision =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "NO ADMISION");

        var estado =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "ESTADO DE DTO");

        var facturador =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "FACTURADOR");

        ValidarCamposObligatorios(
            fila,
            fe,
            prefijo,
            numeroFactura,
            aseguradora,
            tipoDocumento,
            numeroDocumento,
            nombreCompleto,
            atencion,
            costo,
            estado,
            facturador,
            inconsistencias);

        ValidarLongitudes(
            fila,
            fe,
            prefijo,
            numeroFactura,
            numeroDocumento,
            nombreCompleto,
            numeroAdmision,
            inconsistencias);

        ValidarCorrespondenciaFe(
            fila,
            fe,
            prefijo,
            numeroFactura,
            inconsistencias);

        ValidarDuplicado(
            fila,
            fe,
            facturasEncontradas,
            inconsistencias);

        var fechaFacturaValida =
            ValidarFechaFactura(
                hoja,
                fila,
                columnas["FECHA FACTURA"],
                inconsistencias,
                out var fechaFactura);

        if (fechaFacturaValida)
        {
            aniosDetectados.Add(
                fechaFactura.Year);
        }

        ValidarValorFactura(
            hoja,
            fila,
            columnas["VALOR"],
            inconsistencias);

        ValidarCatalogo(
            aseguradora,
            fila,
            "ASEGURADORA",
            "CATALOGO_ASEGURADORA_NO_MAPEADO",
            indicesCatalogos.Aseguradoras,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            tipoDocumento,
            fila,
            "TIPO DTO",
            "CATALOGO_TIPO_DOCUMENTO_NO_MAPEADO",
            indicesCatalogos.TiposDocumento,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            atencion,
            fila,
            "ATENCION",
            "CATALOGO_ATENCION_NO_MAPEADO",
            indicesCatalogos.Atenciones,
            catalogosNoMapeados,
            inconsistencias);

        ValidarCatalogo(
            costo,
            fila,
            "COSTO",
            "CATALOGO_COSTO_NO_MAPEADO",
            indicesCatalogos.Costos,
            catalogosNoMapeados,
            inconsistencias);

        var estadoId =
            ValidarCatalogo(
                estado,
                fila,
                "ESTADO DE DTO",
                "CATALOGO_ESTADO_NO_MAPEADO",
                indicesCatalogos.Estados,
                catalogosNoMapeados,
                inconsistencias);

        ValidarCatalogo(
            facturador,
            fila,
            "FACTURADOR",
            "CATALOGO_FACTURADOR_NO_MAPEADO",
            indicesCatalogos.Facturadores,
            catalogosNoMapeados,
            inconsistencias);

        ValidarFechaRadicacion(
            hoja,
            fila,
            columnas["FECHA DE RADICACION"],
            fechaFacturaValida,
            fechaFactura,
            estadoId == CodigosEstadoFactura.Anulada,
            inconsistencias);

        ValidarFechaAdmision(
            hoja,
            fila,
            columnas["FECHA ADMISION"],
            fechaFacturaValida,
            fechaFactura,
            inconsistencias);
    }

    private static void ValidarCamposObligatorios(
        int fila,
        string fe,
        string prefijo,
        string numeroFactura,
        string aseguradora,
        string tipoDocumento,
        string numeroDocumento,
        string nombreCompleto,
        string atencion,
        string costo,
        string estado,
        string facturador,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        ValidarTextoRequerido(
            fe,
            fila,
            "FE",
            "FE_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            prefijo,
            fila,
            "PREFIJO",
            "PREFIJO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            numeroFactura,
            fila,
            "FACTURA",
            "FACTURA_REQUERIDA",
            inconsistencias);

        ValidarTextoRequerido(
            aseguradora,
            fila,
            "ASEGURADORA",
            "ASEGURADORA_REQUERIDA",
            inconsistencias);

        ValidarTextoRequerido(
            tipoDocumento,
            fila,
            "TIPO DTO",
            "TIPO_DOCUMENTO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            numeroDocumento,
            fila,
            "NUMERO DTO",
            "NUMERO_DOCUMENTO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            nombreCompleto,
            fila,
            "NOMBRE COMPLETO",
            "NOMBRE_COMPLETO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            atencion,
            fila,
            "ATENCION",
            "ATENCION_REQUERIDA",
            inconsistencias);

        ValidarTextoRequerido(
            costo,
            fila,
            "COSTO",
            "COSTO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            estado,
            fila,
            "ESTADO DE DTO",
            "ESTADO_REQUERIDO",
            inconsistencias);

        ValidarTextoRequerido(
            facturador,
            fila,
            "FACTURADOR",
            "FACTURADOR_REQUERIDO",
            inconsistencias);
    }

    private static void ValidarLongitudes(
        int fila,
        string fe,
        string prefijo,
        string numeroFactura,
        string numeroDocumento,
        string nombreCompleto,
        string numeroAdmision,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        ValidarLongitud(
            fe,
            Factura.IdLongitudMaxima,
            fila,
            "FE",
            "FE_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            prefijo,
            Factura.PrefijoLongitudMaxima,
            fila,
            "PREFIJO",
            "PREFIJO_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            numeroFactura,
            Factura.NumeroLongitudMaxima,
            fila,
            "FACTURA",
            "FACTURA_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            numeroDocumento,
            Factura.NumeroDocumentoLongitudMaxima,
            fila,
            "NUMERO DTO",
            "NUMERO_DOCUMENTO_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            nombreCompleto,
            Factura.NombreCompletoLongitudMaxima,
            fila,
            "NOMBRE COMPLETO",
            "NOMBRE_COMPLETO_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            numeroAdmision,
            Factura.NumeroAdmisionLongitudMaxima,
            fila,
            "NO ADMISION",
            "NUMERO_ADMISION_LONGITUD_EXCEDIDA",
            inconsistencias);
    }

    private static void ValidarCorrespondenciaFe(
        int fila,
        string fe,
        string prefijo,
        string numeroFactura,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(fe) ||
            string.IsNullOrWhiteSpace(prefijo) ||
            string.IsNullOrWhiteSpace(numeroFactura))
        {
            return;
        }

        var feEsperado =
            $"{prefijo.Trim()}{numeroFactura.Trim()}";

        if (string.Equals(
                fe.Trim(),
                feEsperado,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AgregarError(
            inconsistencias,
            fila,
            "FE",
            "FE_NO_COINCIDE",
            "El identificador FE no coincide con la " +
            "combinación de PREFIJO y FACTURA.");
    }

    private static void ValidarDuplicado(
        int fila,
        string fe,
        ISet<string> facturasEncontradas,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(fe))
        {
            return;
        }

        var feNormalizado =
            fe.Trim().ToUpperInvariant();

        if (facturasEncontradas.Add(
                feNormalizado))
        {
            return;
        }

        AgregarError(
            inconsistencias,
            fila,
            "FE",
            "FACTURA_DUPLICADA",
            "El identificador FE ya apareció " +
            "anteriormente en el archivo.");
    }

    private static bool ValidarFechaFactura(
        IXLWorksheet hoja,
        int fila,
        int columna,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        out DateOnly fechaFactura)
    {
        fechaFactura = default;

        var celda =
            hoja.Cell(fila, columna);

        if (string.IsNullOrWhiteSpace(
                ObtenerTextoCelda(celda)))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA FACTURA",
                "FECHA_FACTURA_REQUERIDA",
                "La fila no contiene la fecha de factura.");

            return false;
        }

        if (IntentarObtenerFecha(
                celda,
                out fechaFactura))
        {
            return true;
        }

        AgregarError(
            inconsistencias,
            fila,
            "FECHA FACTURA",
            "FECHA_FACTURA_INVALIDA",
            "El valor no corresponde a una fecha válida.");

        return false;
    }

    private static void ValidarValorFactura(
        IXLWorksheet hoja,
        int fila,
        int columna,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var celda =
            hoja.Cell(fila, columna);

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
        int columna,
        bool fechaFacturaValida,
        DateOnly fechaFactura,
        bool facturaAnulada,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var celda =
            hoja.Cell(fila, columna);

        var texto =
            ObtenerTextoCelda(celda);

        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        if (facturaAnulada)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE RADICACION",
                "FECHA_RADICACION_FACTURA_ANULADA",
                "Una factura anulada debe tener vacía " +
                "la fecha de radicación.");

            return;
        }

        if (!IntentarObtenerFecha(
                celda,
                out var fechaRadicacion))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE RADICACION",
                "FECHA_RADICACION_INVALIDA",
                "El valor no corresponde a una fecha válida.");

            return;
        }

        if (fechaRadicacion.Year == 1900)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE RADICACION",
                "FECHA_RADICACION_SENTINELA_NO_PERMITIDA",
                "No debe utilizarse una fecha del año 1900 " +
                "para representar una fecha vacía.");

            return;
        }

        if (fechaFacturaValida &&
            fechaRadicacion < fechaFactura)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE RADICACION",
                "FECHA_RADICACION_ANTERIOR",
                "La fecha de radicación no puede ser " +
                "anterior a la fecha de factura.");
        }
    }

    private static void ValidarFechaAdmision(
        IXLWorksheet hoja,
        int fila,
        int columna,
        bool fechaFacturaValida,
        DateOnly fechaFactura,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var celda =
            hoja.Cell(fila, columna);

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
                "FECHA ADMISION",
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
                "FECHA ADMISION",
                "FECHA_ADMISION_POSTERIOR",
                "La fecha de admisión no puede ser " +
                "posterior a la fecha de factura.");
        }
    }

    private static int? ValidarCatalogo(
        string valor,
        int fila,
        string columna,
        string codigo,
        IReadOnlyDictionary<string, int> valoresValidos,
        ISet<string> catalogosNoMapeados,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var valorNormalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(valor);

        if (valoresValidos.TryGetValue(
                valorNormalizado,
                out var identificador))
        {
            return identificador;
        }

        var clave =
            $"{codigo}:{valorNormalizado}";

        catalogosNoMapeados.Add(clave);

        AgregarError(
            inconsistencias,
            fila,
            columna,
            codigo,
            "El valor utilizado no existe en el " +
            "catálogo normalizado.",
            SanitizadorValorPresentadoImportacion
                .Sanitizar(valor));

        return null;
    }

    private static IndicesCatalogos CrearIndicesCatalogos(
        CatalogosImportacionDto catalogos)
    {
        return new IndicesCatalogos(
            CrearIndice(catalogos.Aseguradoras),
            CrearIndice(catalogos.TiposDocumento),
            CrearIndice(catalogos.Atenciones),
            CrearIndice(catalogos.Costos),

            CrearIndice(
                catalogos.Estados,
                incluirIdentificadores: true),

            CrearIndice(catalogos.Facturadores));
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

    private static void ValidarTextoRequerido(
        string texto,
        int fila,
        string columna,
        string codigo,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        AgregarError(
            inconsistencias,
            fila,
            columna,
            codigo,
            "La fila no contiene el dato obligatorio.");
    }

    private static void ValidarLongitud(
        string texto,
        int longitudMaxima,
        int fila,
        string columna,
        string codigo,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(texto) ||
            texto.Trim().Length <= longitudMaxima)
        {
            return;
        }

        AgregarError(
            inconsistencias,
            fila,
            columna,
            codigo,
            $"El valor no puede superar los " +
            $"{longitudMaxima} caracteres.");
    }

    private static bool TieneIdentificacionFactura(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas)
    {
        return
            !string.IsNullOrWhiteSpace(
                ObtenerTexto(
                    hoja,
                    fila,
                    columnas,
                    "FE"))
            ||
            !string.IsNullOrWhiteSpace(
                ObtenerTexto(
                    hoja,
                    fila,
                    columnas,
                    "PREFIJO"))
            ||
            !string.IsNullOrWhiteSpace(
                ObtenerTexto(
                    hoja,
                    fila,
                    columnas,
                    "FACTURA"));
    }

    private static bool EsFilaConDatos(
        IXLWorksheet hoja,
        int fila,
        IEnumerable<int> columnas)
    {
        return columnas.Any(
            columna =>
                !string.IsNullOrWhiteSpace(
                    ObtenerTextoCelda(
                        hoja.Cell(fila, columna))));
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombreColumna)
    {
        return ObtenerTextoCelda(
            hoja.Cell(
                fila,
                columnas[nombreColumna]));
    }

    private static string ObtenerTextoCelda(
        IXLCell celda)
    {
        return celda.CachedValue
            .ToString()
            .Trim();
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
            ObtenerTextoCelda(celda);

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

    private static void ValidarInspeccion(
        ResultadoInspeccionPlantillaDto inspeccion)
    {
        if (!inspeccion.EsValida ||
            inspeccion.TipoDetectado !=
            TipoImportacion.Facturas)
        {
            throw new InvalidOperationException(
                "La validación detallada requiere una " +
                "inspección estructural válida de facturas.");
        }

        if (string.IsNullOrWhiteSpace(
                inspeccion.NombreHojaDatos))
        {
            throw new InvalidOperationException(
                "La inspección no identificó la hoja de datos.");
        }
    }

    private static void AgregarError(
        ICollection<InconsistenciaImportacionDto>
            inconsistencias,
        int fila,
        string columna,
        string codigo,
        string mensaje,
        string? valorPresentado = null)
    {
        inconsistencias.Add(
            new InconsistenciaImportacionDto
            {
                Fila = fila,
                Columna = columna,
                Codigo = codigo,
                Mensaje = mensaje,
                ValorPresentado = valorPresentado,

                Severidad =
                    SeveridadInconsistenciaImportacion
                        .Error
            });
    }

    private sealed record IndicesCatalogos(
        IReadOnlyDictionary<string, int> Aseguradoras,
        IReadOnlyDictionary<string, int> TiposDocumento,
        IReadOnlyDictionary<string, int> Atenciones,
        IReadOnlyDictionary<string, int> Costos,
        IReadOnlyDictionary<string, int> Estados,
        IReadOnlyDictionary<string, int> Facturadores);
}
