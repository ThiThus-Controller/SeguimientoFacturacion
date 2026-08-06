using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure
    .Services.Importacion;

/// <summary>
/// Valida una plantilla modular de pagos mediante
/// ClosedXML, sin modificar el archivo ni escribir
/// información en la base de datos.
/// </summary>
public sealed class ValidadorPagosModularClosedXml :
    IValidadorPagosModular
{
    private readonly IInspectorEstructuraPlantilla
        _inspector;

    private readonly IConsultaCatalogosImportacion
        _consultaCatalogos;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    /// <summary>
    /// Inicializa el validador modular de pagos.
    /// </summary>
    public ValidadorPagosModularClosedXml(
        IInspectorEstructuraPlantilla inspector,
        IConsultaCatalogosImportacion consultaCatalogos,
        IConsultaReferenciasFacturasImportacion
            consultaFacturas)
    {
        ArgumentNullException.ThrowIfNull(inspector);

        ArgumentNullException.ThrowIfNull(
            consultaCatalogos);

        ArgumentNullException.ThrowIfNull(
            consultaFacturas);

        _inspector = inspector;
        _consultaCatalogos = consultaCatalogos;
        _consultaFacturas = consultaFacturas;
    }

    /// <inheritdoc />
    public async Task<ResultadoValidacionPagosDto>
        ValidarAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        ArgumentNullException.ThrowIfNull(
            solicitud.Contenido);

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
            return CrearResultadoEstructuralInvalido(
                solicitud.NombreArchivo,
                inspeccion);
        }

        var catalogos =
            await _consultaCatalogos.ObtenerAsync(
                cancellationToken);

        var indiceAseguradoras =
            CrearIndiceCatalogo(
                catalogos.Aseguradoras);

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

        var inconsistencias =
            inspeccion.Inconsistencias.ToList();

        var catalogosNoMapeados =
            new HashSet<string>(
                StringComparer.Ordinal);

        var filas = new List<FilaPago>();

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

            filas.Add(
                ValidarFila(
                    hoja,
                    fila,
                    inspeccion.Columnas,
                    indiceAseguradoras,
                    catalogosNoMapeados,
                    inconsistencias));
        }

        if (filas.Count == 0)
        {
            AgregarError(
                inconsistencias,
                inspeccion.PrimeraFilaDatos,
                "ARCHIVO",
                "PLANTILLA_SIN_DATOS",
                "La plantilla no contiene filas de pagos.");
        }

        var referencias =
            await _consultaFacturas.ObtenerPorIdsAsync(
                filas
                    .Where(
                        fila =>
                            !string.IsNullOrWhiteSpace(
                                fila.IdentificadorFe))
                    .Select(
                        fila =>
                            fila.IdentificadorFe)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                cancellationToken);

        ValidarReferencias(
            filas,
            referencias,
            inconsistencias);

        ValidarGruposPago(
            filas,
            inconsistencias);

        return new ResultadoValidacionPagosDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

            TotalFilasAnalizadas =
                filas.Count,

            PagosDetectados =
                ContarPagos(filas),

            AplicacionesDetectadas =
                filas.Count,

            CatalogosNoMapeados =
                catalogosNoMapeados.Count,

            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static FilaPago ValidarFila(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        IReadOnlyDictionary<string, int>
            indiceAseguradoras,
        ISet<string> catalogosNoMapeados,
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

        var factura =
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

        var recibo =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "RECIBO");

        var notas =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "NOTAS");

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
            factura,
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
            recibo,
            fila,
            "RECIBO",
            "RECIBO_REQUERIDO",
            inconsistencias);

        ValidarLongitud(
            fe,
            AplicacionPago.FacturaIdLongitudMaxima,
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
            factura,
            Factura.NumeroLongitudMaxima,
            fila,
            "FACTURA",
            "FACTURA_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            recibo,
            Pago.ReciboLongitudMaxima,
            fila,
            "RECIBO",
            "RECIBO_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarLongitud(
            notas,
            Pago.NotasLongitudMaxima,
            fila,
            "NOTAS",
            "NOTAS_LONGITUD_EXCEDIDA",
            inconsistencias);

        ValidarCorrespondenciaFe(
            fila,
            fe,
            prefijo,
            factura,
            inconsistencias);

        var aseguradoraId =
            ResolverAseguradora(
                aseguradora,
                fila,
                indiceAseguradoras,
                catalogosNoMapeados,
                inconsistencias);

        var fechaPago =
            ObtenerFechaObligatoria(
                hoja.Cell(
                    fila,
                    columnas["FECHA DE PAGO"]),
                fila,
                inconsistencias);

        var valorPagado =
            ObtenerImporte(
                hoja.Cell(
                    fila,
                    columnas["VALOR PAGADO"]),
                fila,
                "VALOR PAGADO",
                debeSerPositivo: true,
                permiteVacio: false,
                inconsistencias);

        var retencion =
            ObtenerImporte(
                hoja.Cell(
                    fila,
                    columnas["RETENCION"]),
                fila,
                "RETENCION",
                debeSerPositivo: false,
                permiteVacio: true,
                inconsistencias);

        var reteIca =
            ObtenerImporte(
                hoja.Cell(
                    fila,
                    columnas["RETE ICA"]),
                fila,
                "RETE ICA",
                debeSerPositivo: false,
                permiteVacio: true,
                inconsistencias);

        return new FilaPago(
            NumeroFila: fila,

            IdentificadorFe:
                NormalizarTexto(fe),

            AseguradoraId:
                aseguradoraId,

            FechaPago:
                fechaPago,

            Recibo:
                NormalizarTexto(recibo),

            ValorPagado:
                valorPagado,

            Retencion:
                retencion,

            ReteIca:
                reteIca,

            Notas:
                NormalizarTextoOpcional(notas));
    }

    private static void ValidarReferencias(
        IEnumerable<FilaPago> filas,
        IEnumerable<ReferenciaFacturaImportacionDto>
            referencias,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var referenciasMaterializadas =
            referencias.ToArray();

        var referenciasDuplicadas =
            referenciasMaterializadas
                .GroupBy(
                    referencia =>
                        referencia.FacturaId,
                    StringComparer.OrdinalIgnoreCase)
                .Where(
                    grupo =>
                        grupo.Count() > 1)
                .Select(
                    grupo =>
                        grupo.Key)
                .ToArray();

        if (referenciasDuplicadas.Length > 0)
        {
            throw new InvalidOperationException(
                "La consulta de facturas devolvió " +
                "identificadores duplicados.");
        }

        var indice =
            referenciasMaterializadas.ToDictionary(
                referencia =>
                    referencia.FacturaId,
                StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            if (string.IsNullOrWhiteSpace(
                    fila.IdentificadorFe))
            {
                continue;
            }

            if (!indice.TryGetValue(
                    fila.IdentificadorFe,
                    out var factura))
            {
                AgregarError(
                    inconsistencias,
                    fila.NumeroFila,
                    "FE",
                    "FACTURA_NO_EXISTE",
                    "La factura relacionada no existe.");

                continue;
            }

            if (fila.AseguradoraId.HasValue &&
                fila.AseguradoraId.Value !=
                factura.AseguradoraId)
            {
                AgregarError(
                    inconsistencias,
                    fila.NumeroFila,
                    "ASEGURADORA",
                    "ASEGURADORA_NO_COINCIDE_FACTURA",
                    "La aseguradora indicada no corresponde " +
                    "a la aseguradora de la factura.");
            }

        }
    }

    private static void ValidarGruposPago(
        IEnumerable<FilaPago> filas,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var grupos =
            filas
                .Where(
                    fila =>
                        fila.AseguradoraId.HasValue &&
                        !string.IsNullOrWhiteSpace(
                            fila.Recibo))
                .GroupBy(
                    fila =>
                        CrearClavePago(fila),
                    StringComparer.OrdinalIgnoreCase);

        foreach (var grupo in grupos)
        {
            var filasPago =
                grupo
                    .OrderBy(
                        fila =>
                            fila.NumeroFila)
                    .ToArray();

            ValidarAplicacionesDuplicadas(
                filasPago,
                inconsistencias);

            ValidarDatosCompartidos(
                filasPago,
                inconsistencias);
        }
    }

    private static bool ValidarDatosCompartidos(
        IReadOnlyList<FilaPago> filas,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var referencia = filas[0];
        var esCoherente = true;

        foreach (var fila in filas.Skip(1))
        {
            esCoherente &=
                ValidarDatoCompartido(
                    referencia.FechaPago ==
                    fila.FechaPago,
                    fila.NumeroFila,
                    "FECHA DE PAGO",
                    inconsistencias);

            esCoherente &=
                ValidarDatoCompartido(
                    string.Equals(
                        referencia.Notas,
                        fila.Notas,
                        StringComparison.OrdinalIgnoreCase),
                    fila.NumeroFila,
                    "NOTAS",
                    inconsistencias);
        }

        return esCoherente;
    }

    private static bool ValidarDatoCompartido(
        bool coincide,
        int fila,
        string columna,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (coincide)
        {
            return true;
        }

        AgregarError(
            inconsistencias,
            fila,
            columna,
            "DATOS_PAGO_INCONSISTENTES",
            "El dato no coincide con las demás filas " +
            "del mismo recibo y aseguradora.");

        return false;
    }

    private static void ValidarAplicacionesDuplicadas(
        IReadOnlyCollection<FilaPago> filas,
        ICollection<InconsistenciaImportacionDto>
                       inconsistencias)
    {
        var duplicadas =
            filas
                .Where(
                    fila =>
                        !string.IsNullOrWhiteSpace(
                            fila.IdentificadorFe))
                .GroupBy(
                    fila =>
                        fila.IdentificadorFe,
                    StringComparer.OrdinalIgnoreCase)
                .Where(
                    grupo =>
                        grupo.Count() > 1);

        foreach (var grupo in duplicadas)
        {
            foreach (var fila in grupo.Skip(1))
            {
                AgregarError(
                    inconsistencias,
                    fila.NumeroFila,
                    "FE",
                    "APLICACION_PAGO_DUPLICADA",
                    "El recibo contiene más de una " +
                    "aplicación para la misma factura.");
            }
        }
    }

    private static int ContarPagos(
        IEnumerable<FilaPago> filas)
    {
        return filas
            .Where(
                fila =>
                    fila.AseguradoraId.HasValue &&
                    !string.IsNullOrWhiteSpace(
                        fila.Recibo))
            .Select(
                CrearClavePago)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    private static string CrearClavePago(
        FilaPago fila)
    {
        return
            $"{fila.AseguradoraId!.Value}|" +
            fila.Recibo;
    }

    private static int? ResolverAseguradora(
        string valor,
        int fila,
        IReadOnlyDictionary<string, int> indice,
        ISet<string> noMapeados,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(valor);

        if (indice.TryGetValue(
                normalizado,
                out var identificador))
        {
            return identificador;
        }

        noMapeados.Add(normalizado);

        AgregarError(
            inconsistencias,
            fila,
            "ASEGURADORA",
            "CATALOGO_ASEGURADORA_NO_MAPEADO",
            "La aseguradora no existe en el catálogo.",
            SanitizadorValorPresentadoImportacion
                .Sanitizar(valor));

        return null;
    }

    private static DateOnly? ObtenerFechaObligatoria(
        IXLCell celda,
        int fila,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var texto =
            celda.CachedValue.ToString().Trim();

        if (string.IsNullOrWhiteSpace(texto))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA DE PAGO",
                "FECHA_PAGO_REQUERIDA",
                "La fecha del pago es obligatoria.");

            return null;
        }

        if (IntentarObtenerFecha(
                celda,
                out var fecha))
        {
            return fecha;
        }

        AgregarError(
            inconsistencias,
            fila,
            "FECHA DE PAGO",
            "FECHA_PAGO_INVALIDA",
            "El valor no corresponde a una fecha válida.");

        return null;
    }

    private static decimal? ObtenerImporte(
        IXLCell celda,
        int fila,
        string columna,
        bool debeSerPositivo,
        bool permiteVacio,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var texto =
            celda.CachedValue.ToString().Trim();

        var codigoColumna =
            columna.Replace(
                ' ',
                '_');

        if (string.IsNullOrWhiteSpace(texto))
        {
            if (permiteVacio)
            {
                return decimal.Zero;
            }

            AgregarError(
                inconsistencias,
                fila,
                columna,
                $"{codigoColumna}_REQUERIDO",
                "El valor monetario es obligatorio.");

            return null;
        }

        if (!IntentarObtenerDecimal(
                celda,
                out var valor))
        {
            AgregarError(
                inconsistencias,
                fila,
                columna,
                $"{codigoColumna}_INVALIDO",
                "El valor no corresponde a un " +
                "importe monetario válido.");

            return null;
        }

        if (decimal.Round(valor, 2) != valor)
        {
            AgregarError(
                inconsistencias,
                fila,
                columna,
                $"{codigoColumna}_MAS_DOS_DECIMALES",
                "El valor monetario no puede contener " +
                "más de dos decimales.");

            return null;
        }

        if (debeSerPositivo &&
            valor <= decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                columna,
                $"{codigoColumna}_NO_POSITIVO",
                "El valor debe ser mayor que cero.");

            return null;
        }

        if (!debeSerPositivo &&
            valor < decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                columna,
                $"{codigoColumna}_NEGATIVO",
                "El valor no puede ser negativo.");

            return null;
        }

        return valor;
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
            celda.CachedValue.ToString().Trim();

        return decimal.TryParse(
                   texto,
                   NumberStyles.Number |
                   NumberStyles.AllowCurrencySymbol,
                   CultureInfo.GetCultureInfo("es-CO"),
                   out valor)
               ||
               decimal.TryParse(
                   texto,
                   NumberStyles.Number |
                   NumberStyles.AllowCurrencySymbol,
                   CultureInfo.InvariantCulture,
                   out valor);
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
            celda.CachedValue.ToString().Trim();

        if (DateOnly.TryParse(
                texto,
                CultureInfo.GetCultureInfo("es-CO"),
                DateTimeStyles.None,
                out fecha))
        {
            return true;
        }

        if (DateTime.TryParse(
                texto,
                CultureInfo.GetCultureInfo("es-CO"),
                DateTimeStyles.None,
                out fechaHora))
        {
            fecha =
                DateOnly.FromDateTime(fechaHora);

            return true;
        }

        fecha = default;
        return false;
    }

    private static void ValidarCorrespondenciaFe(
        int fila,
        string fe,
        string prefijo,
        string factura,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(fe) ||
            string.IsNullOrWhiteSpace(prefijo) ||
            string.IsNullOrWhiteSpace(factura))
        {
            return;
        }

        var esperado =
            $"{prefijo.Trim()}{factura.Trim()}";

        if (!string.Equals(
                fe.Trim(),
                esperado,
                StringComparison.OrdinalIgnoreCase))
        {
            AgregarError(
                inconsistencias,
                fila,
                "FE",
                "FE_NO_COINCIDE",
                "FE no coincide con PREFIJO y FACTURA.");
        }
    }

    private static void ValidarTextoRequerido(
        string valor,
        int fila,
        string columna,
        string codigo,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            AgregarError(
                inconsistencias,
                fila,
                columna,
                codigo,
                "La fila no contiene el dato obligatorio.");
        }
    }

    private static void ValidarLongitud(
        string valor,
        int longitudMaxima,
        int fila,
        string columna,
        string codigo,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!string.IsNullOrWhiteSpace(valor) &&
            valor.Trim().Length > longitudMaxima)
        {
            AgregarError(
                inconsistencias,
                fila,
                columna,
                codigo,
                $"El valor no puede superar los " +
                $"{longitudMaxima} caracteres.");
        }
    }

    private static IReadOnlyDictionary<string, int>
        CrearIndiceCatalogo(
            IEnumerable<ReferenciaCatalogoImportacionDto>
                referencias)
    {
        var elementos =
            referencias
                .Where(
                    referencia =>
                        !string.IsNullOrWhiteSpace(
                            referencia.Valor))
                .Select(
                    referencia =>
                        new
                        {
                            referencia.Id,

                            Valor =
                                NormalizadorEncabezadoImportacion
                                    .Normalizar(
                                        referencia.Valor)
                        })
                .ToArray();

        var duplicados =
            elementos
                .GroupBy(
                    elemento =>
                        elemento.Valor,
                    StringComparer.Ordinal)
                .Where(
                    grupo =>
                        grupo.Count() > 1)
                .Select(
                    grupo =>
                        grupo.Key)
                .ToArray();

        if (duplicados.Length > 0)
        {
            throw new InvalidOperationException(
                "El catálogo de aseguradoras contiene " +
                "valores normalizados duplicados.");
        }

        return elementos.ToDictionary(
            elemento =>
                elemento.Valor,
            elemento =>
                elemento.Id,
            StringComparer.Ordinal);
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string columna)
    {
        return hoja
            .Cell(
                fila,
                columnas[columna])
            .CachedValue
            .ToString()
            .Trim();
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
                        .ToString()));
    }

    private static string NormalizarTexto(
        string valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? string.Empty
            : valor.Trim().ToUpperInvariant();
    }

    private static string? NormalizarTextoOpcional(
        string valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    private static async Task<MemoryStream>
        CopiarContenidoAsync(
            Stream contenido,
            CancellationToken cancellationToken)
    {
        if (contenido.CanSeek)
        {
            contenido.Position = 0;
        }

        var copia = new MemoryStream();

        await contenido.CopyToAsync(
            copia,
            cancellationToken);

        copia.Position = 0;

        return copia;
    }

    private static ResultadoValidacionPagosDto
        CrearResultadoEstructuralInvalido(
            string nombreArchivo,
            ResultadoInspeccionPlantillaDto inspeccion)
    {
        return new ResultadoValidacionPagosDto
        {
            NombreArchivo =
                nombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

            TotalFilasAnalizadas = 0,
            PagosDetectados = 0,
            AplicacionesDetectadas = 0,
            CatalogosNoMapeados = 0,

            Inconsistencias =
                inspeccion.Inconsistencias
        };
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

    private sealed record FilaPago(
        int NumeroFila,
        string IdentificadorFe,
        int? AseguradoraId,
        DateOnly? FechaPago,
        string Recibo,
        decimal? ValorPagado,
        decimal? Retencion,
        decimal? ReteIca,
        string? Notas);
}
