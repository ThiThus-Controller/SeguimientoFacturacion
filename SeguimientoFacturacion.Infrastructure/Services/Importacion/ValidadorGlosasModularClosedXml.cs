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
/// Valida una plantilla modular de glosas
/// mediante ClosedXML.
/// </summary>
public sealed class
    ValidadorGlosasModularClosedXml :
        IValidadorGlosasModular
{
    private readonly IInspectorEstructuraPlantilla
        _inspector;

    private readonly IConsultaCatalogosImportacion
        _consultaCatalogos;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    /// <summary>
    /// Inicializa el validador modular de glosas.
    /// </summary>
    public ValidadorGlosasModularClosedXml(
        IInspectorEstructuraPlantilla inspector,
        IConsultaCatalogosImportacion
            consultaCatalogos,
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
    public async Task<ResultadoValidacionGlosasDto>
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
                TipoImportacion.Glosas,
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

        var clavesGlosas =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var filas = new List<FilaGlosa>();

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

            var filaValidada =
                ValidarFila(
                    hoja,
                    fila,
                    inspeccion.Columnas,
                    indiceAseguradoras,
                    clavesGlosas,
                    catalogosNoMapeados,
                    inconsistencias);

            filas.Add(filaValidada);
        }

        if (filas.Count == 0)
        {
            AgregarError(
                inconsistencias,
                inspeccion.PrimeraFilaDatos,
                "ARCHIVO",
                "PLANTILLA_SIN_DATOS",
                "La plantilla no contiene filas de glosas.");
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

        return new ResultadoValidacionGlosasDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

            TotalFilasAnalizadas =
                filas.Count,

            GlosasDetectadas =
                filas.Count,

            GlosasConRespuestaDetectadas =
                filas.Count(
                    fila =>
                        fila.FechaRespuesta.HasValue),

            CatalogosNoMapeados =
                catalogosNoMapeados.Count,

            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static FilaGlosa ValidarFila(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        IReadOnlyDictionary<string, int>
            indiceAseguradoras,
        ISet<string> clavesGlosas,
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

        fe = ResolverIdentificadorFe(
            hoja.Cell(fila, columnas["FE"]),
            fe,
            prefijo,
            factura);

        var aseguradora =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "ASEGURADORA");

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

        ValidarLongitud(
            fe,
            Glosa.FacturaIdLongitudMaxima,
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

        var fechaGlosa =
            ObtenerFechaObligatoria(
                hoja.Cell(
                    fila,
                    columnas["FECHA GLOSA"]),
                fila,
                "FECHA GLOSA",
                "FECHA_GLOSA_REQUERIDA",
                "FECHA_GLOSA_INVALIDA",
                inconsistencias);

        var valorGlosa =
            ObtenerValorGlosa(
                hoja.Cell(
                    fila,
                    columnas["VALOR GLOSA"]),
                fila,
                inconsistencias);

        var fechaRespuesta =
            ObtenerFechaRespuesta(
                hoja.Cell(
                    fila,
                    columnas["FECHA RTA GLOSA"]),
                fila,
                inconsistencias);

        var estado =
            ObtenerEstadoGlosa(
                hoja.Cell(
                    fila,
                    columnas["ESTADO GLOSA"]),
                fila,
                inconsistencias);

        var valorAceptado =
            ObtenerValorAceptado(
                hoja.Cell(
                    fila,
                    columnas["VALOR ACEPTADO"]),
                fila,
                out var valorAceptadoInformado,
                inconsistencias);

        if (fechaGlosa.HasValue &&
            fechaRespuesta.HasValue &&
            fechaRespuesta.Value <
            fechaGlosa.Value)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA RTA GLOSA",
                "FECHA_RESPUESTA_ANTERIOR_GLOSA",
                "La fecha de respuesta no puede ser " +
                "anterior a la fecha de la glosa.");
        }

        ValidarResolucion(
            estado,
            fechaRespuesta,
            valorGlosa,
            valorAceptado,
            valorAceptadoInformado,
            fila,
            inconsistencias);

        RegistrarClaveGlosa(
            fe,
            fechaGlosa,
            valorGlosa,
            fila,
            clavesGlosas,
            inconsistencias);

        return new FilaGlosa(
            NumeroFila: fila,

            IdentificadorFe:
                fe.Trim().ToUpperInvariant(),

            AseguradoraId:
                aseguradoraId,

            FechaGlosa:
                fechaGlosa,

            ValorGlosa:
                valorGlosa,

            FechaRespuesta:
                fechaRespuesta,

            Estado:
                estado,

            ValorAceptado:
                valorAceptado);
    }

    private static string ResolverIdentificadorFe(
        IXLCell celda,
        string valor,
        string prefijo,
        string numeroFactura)
    {
        if (!string.IsNullOrWhiteSpace(valor) ||
            string.IsNullOrWhiteSpace(celda.FormulaA1) ||
            string.IsNullOrWhiteSpace(prefijo) ||
            string.IsNullOrWhiteSpace(numeroFactura))
        {
            return valor;
        }

        return $"{prefijo.Trim()}{numeroFactura.Trim()}";
    }

    private static EstadoGlosa? ObtenerEstadoGlosa(
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
                "ESTADO GLOSA",
                "ESTADO_GLOSA_REQUERIDO",
                "El estado de la glosa es obligatorio.");

            return null;
        }

        var normalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(texto);

        var estado = normalizado switch
        {
            "1" or "ABIERTA" => EstadoGlosa.Abierta,
            "2" or "RESPONDIDA" => EstadoGlosa.Respondida,
            "3" or "ACEPTADA" => EstadoGlosa.Aceptada,
            "4" or "LEVANTADA" => EstadoGlosa.Levantada,
            "5" or "CONCILIADA" => EstadoGlosa.Conciliada,
            _ => (EstadoGlosa?)null
        };

        if (!estado.HasValue)
        {
            AgregarError(
                inconsistencias,
                fila,
                "ESTADO GLOSA",
                "ESTADO_GLOSA_INVALIDO",
                "El estado debe ser ABIERTA, RESPONDIDA, " +
                "ACEPTADA, LEVANTADA o CONCILIADA.",
                SanitizadorValorPresentadoImportacion
                    .Sanitizar(texto));
        }

        return estado;
    }

    private static decimal? ObtenerValorAceptado(
        IXLCell celda,
        int fila,
        out bool valorInformado,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var texto =
            celda.CachedValue.ToString().Trim();

        valorInformado =
            !string.IsNullOrWhiteSpace(texto);

        if (!valorInformado)
        {
            return null;
        }

        if (IntentarObtenerDecimal(celda, out var valor))
        {
            return valor;
        }

        AgregarError(
            inconsistencias,
            fila,
            "VALOR ACEPTADO",
            "VALOR_ACEPTADO_INVALIDO",
            "El valor aceptado no es numérico.",
            SanitizadorValorPresentadoImportacion
                .Sanitizar(texto));

        return null;
    }

    private static void ValidarResolucion(
        EstadoGlosa? estado,
        DateOnly? fechaRespuesta,
        decimal? valorGlosa,
        decimal? valorAceptado,
        bool valorAceptadoInformado,
        int fila,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (!estado.HasValue)
        {
            return;
        }

        var requiereRespuesta =
            estado.Value != EstadoGlosa.Abierta;

        if (requiereRespuesta &&
            !fechaRespuesta.HasValue)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA RTA GLOSA",
                "FECHA_RESPUESTA_REQUERIDA_ESTADO",
                "El estado informado requiere fecha de " +
                "respuesta.");
        }

        if (!requiereRespuesta &&
            fechaRespuesta.HasValue)
        {
            AgregarError(
                inconsistencias,
                fila,
                "FECHA RTA GLOSA",
                "FECHA_RESPUESTA_NO_PERMITIDA_ESTADO",
                "Una glosa abierta no puede tener fecha " +
                "de respuesta.");
        }

        if (estado.Value == EstadoGlosa.Aceptada &&
            !valorAceptadoInformado)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR ACEPTADO",
                "VALOR_ACEPTADO_REQUERIDO",
                "Una glosa aceptada debe informar el valor " +
                "aceptado.");

            return;
        }

        if (!valorAceptado.HasValue)
        {
            return;
        }

        if (valorAceptado.Value < decimal.Zero ||
            valorGlosa.HasValue &&
            valorAceptado.Value > valorGlosa.Value)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR ACEPTADO",
                "VALOR_ACEPTADO_FUERA_RANGO",
                "El valor aceptado debe estar entre cero y " +
                "el valor de la glosa.");
        }

        if (estado.Value == EstadoGlosa.Aceptada &&
            valorAceptado.Value <= decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR ACEPTADO",
                "VALOR_ACEPTADO_NO_POSITIVO",
                "Una glosa aceptada debe tener un valor " +
                "aceptado mayor que cero.");
        }

        if ((estado.Value is
                EstadoGlosa.Abierta or
                EstadoGlosa.Respondida or
                EstadoGlosa.Levantada) &&
            valorAceptado.Value != decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR ACEPTADO",
                "VALOR_ACEPTADO_NO_PERMITIDO_ESTADO",
                "El estado informado no permite un valor " +
                "aceptado diferente de cero.");
        }
    }

    private static void ValidarReferencias(
        IEnumerable<FilaGlosa> filas,
        IEnumerable<ReferenciaFacturaImportacionDto>
            referencias,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var referenciasDuplicadas =
            referencias
                .GroupBy(
                    referencia =>
                        referencia.FacturaId,
                    StringComparer.OrdinalIgnoreCase)
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToArray();

        if (referenciasDuplicadas.Length > 0)
        {
            throw new InvalidOperationException(
                "La consulta de facturas devolvió " +
                "identificadores duplicados.");
        }

        var indice =
            referencias.ToDictionary(
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

            if (fila.FechaGlosa.HasValue &&
                fila.FechaGlosa.Value <
                factura.FechaFactura)
            {
                AgregarError(
                    inconsistencias,
                    fila.NumeroFila,
                    "FECHA GLOSA",
                    "FECHA_GLOSA_ANTERIOR_FACTURA",
                    "La fecha de la glosa no puede ser " +
                    "anterior a la fecha de la factura.");
            }
        }
    }

    private static void RegistrarClaveGlosa(
        string fe,
        DateOnly? fechaGlosa,
        decimal? valorGlosa,
        int fila,
        ISet<string> clavesGlosas,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        if (string.IsNullOrWhiteSpace(fe) ||
            !fechaGlosa.HasValue ||
            !valorGlosa.HasValue ||
            valorGlosa.Value <= decimal.Zero)
        {
            return;
        }

        var clave =
            $"{fe.Trim().ToUpperInvariant()}|" +
            $"{fechaGlosa.Value:yyyy-MM-dd}|" +
            $"{valorGlosa.Value.ToString(
                "0.00",
                CultureInfo.InvariantCulture)}";

        if (!clavesGlosas.Add(clave))
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR GLOSA",
                "GLOSA_DUPLICADA_ARCHIVO",
                "La misma glosa aparece más de una vez " +
                "en el archivo.");
        }
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
        string columna,
        string codigoRequerido,
        string codigoInvalido,
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
                columna,
                codigoRequerido,
                "La fecha es obligatoria.");

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
            columna,
            codigoInvalido,
            "El valor no corresponde a una fecha válida.");

        return null;
    }

    private static DateOnly? ObtenerFechaRespuesta(
        IXLCell celda,
        int fila,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var texto =
            celda.CachedValue.ToString().Trim();

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

        AgregarError(
            inconsistencias,
            fila,
            "FECHA RTA GLOSA",
            "FECHA_RESPUESTA_INVALIDA",
            "El valor no corresponde a una fecha válida.");

        return null;
    }

    private static decimal? ObtenerValorGlosa(
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
                "VALOR GLOSA",
                "VALOR_GLOSA_REQUERIDO",
                "El valor de la glosa es obligatorio.");

            return null;
        }

        if (!IntentarObtenerDecimal(
                celda,
                out var valor))
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR GLOSA",
                "VALOR_GLOSA_INVALIDO",
                "El valor de la glosa no es numérico.");

            return null;
        }

        if (valor <= decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR GLOSA",
                "VALOR_GLOSA_NO_POSITIVO",
                "El valor de la glosa debe ser mayor " +
                "que cero.");
        }

        return valor;
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
                elementos)
    {
        return elementos
            .Where(
                elemento =>
                    !string.IsNullOrWhiteSpace(
                        elemento.Valor))
            .GroupBy(
                elemento =>
                    NormalizadorEncabezadoImportacion
                        .Normalizar(elemento.Valor))
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.First().Id,
                StringComparer.Ordinal);
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

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        string nombre)
    {
        return hoja.Cell(
                fila,
                columnas[nombre])
            .CachedValue
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
            celda.CachedValue.ToString().Trim();

        return DateOnly.TryParse(
                   texto,
                   CultureInfo.GetCultureInfo("es-CO"),
                   DateTimeStyles.AllowWhiteSpaces,
                   out fecha)
               ||
               DateOnly.TryParse(
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

    private static async Task<MemoryStream>
        CopiarContenidoAsync(
            Stream contenido,
            CancellationToken cancellationToken)
    {
        if (!contenido.CanRead)
        {
            throw new ArgumentException(
                "El contenido no puede leerse.",
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

    private static ResultadoValidacionGlosasDto
        CrearResultadoEstructuralInvalido(
            string nombreArchivo,
            ResultadoInspeccionPlantillaDto inspeccion)
    {
        return new ResultadoValidacionGlosasDto
        {
            NombreArchivo = nombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

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

    private sealed record FilaGlosa(
        int NumeroFila,
        string IdentificadorFe,
        int? AseguradoraId,
        DateOnly? FechaGlosa,
        decimal? ValorGlosa,
        DateOnly? FechaRespuesta,
        EstadoGlosa? Estado,
        decimal? ValorAceptado);
}
