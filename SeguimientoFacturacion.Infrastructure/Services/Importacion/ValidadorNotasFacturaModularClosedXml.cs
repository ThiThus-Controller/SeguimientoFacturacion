using System.Globalization;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Valida una plantilla modular de notas crédito
/// y débito mediante ClosedXML.
/// </summary>
public sealed class
    ValidadorNotasFacturaModularClosedXml :
    IValidadorNotasFacturaModular
{
    private readonly IInspectorEstructuraPlantilla
        _inspector;

    private readonly IConsultaCatalogosImportacion
        _consultaCatalogos;

    private readonly
        IConsultaReferenciasFacturasImportacion
        _consultaFacturas;

    /// <summary>
    /// Inicializa el validador modular.
    /// </summary>
    public ValidadorNotasFacturaModularClosedXml(
        IInspectorEstructuraPlantilla inspector,
        IConsultaCatalogosImportacion consultaCatalogos,
        IConsultaReferenciasFacturasImportacion
            consultaFacturas)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(consultaCatalogos);
        ArgumentNullException.ThrowIfNull(consultaFacturas);

        _inspector = inspector;
        _consultaCatalogos = consultaCatalogos;
        _consultaFacturas = consultaFacturas;
    }

    /// <inheritdoc />
    public async Task<ResultadoValidacionNotasFacturaDto>
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
                TipoImportacion.NotasFactura,
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

        var clavesNotas =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var filas =
            new List<FilaNota>();

        var notasCredito = 0;
        var notasDebito = 0;

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

            var filaNota =
                ValidarFila(
                    hoja,
                    fila,
                    inspeccion.Columnas,
                    indiceAseguradoras,
                    clavesNotas,
                    catalogosNoMapeados,
                    inconsistencias);

            filas.Add(filaNota);

            if (filaNota.Tipo ==
                TipoNotaFactura.Credito)
            {
                notasCredito++;
            }
            else if (filaNota.Tipo ==
                     TipoNotaFactura.Debito)
            {
                notasDebito++;
            }
        }

        if (filas.Count == 0)
        {
            AgregarError(
                inconsistencias,
                inspeccion.PrimeraFilaDatos,
                "ARCHIVO",
                "PLANTILLA_SIN_DATOS",
                "La plantilla no contiene filas de notas.");
        }

        var referencias =
            await _consultaFacturas.ObtenerPorIdsAsync(
                filas
                    .Where(fila =>
                        !string.IsNullOrWhiteSpace(
                            fila.IdentificadorFe))
                    .Select(fila =>
                        fila.IdentificadorFe)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                cancellationToken);

        ValidarReferencias(
            filas,
            referencias,
            inconsistencias);

        return new ResultadoValidacionNotasFacturaDto
        {
            NombreArchivo =
                solicitud.NombreArchivo.Trim(),

            HojasDetectadas =
                inspeccion.HojasDetectadas,

            TotalFilasAnalizadas =
                filas.Count,

            NotasDetectadas =
                filas.Count,

            NotasCreditoDetectadas =
                notasCredito,

            NotasDebitoDetectadas =
                notasDebito,

            CatalogosNoMapeados =
                catalogosNoMapeados.Count,

            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static FilaNota ValidarFila(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyDictionary<string, int> columnas,
        IReadOnlyDictionary<string, int>
            indiceAseguradoras,
        ISet<string> clavesNotas,
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

        var tipoTexto =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "TIPO NOTA");

        var numeroNota =
            ObtenerTexto(
                hoja,
                fila,
                columnas,
                "NUMERO NOTA");

        ValidarRequerido(
            fe,
            fila,
            "FE",
            "FE_REQUERIDO",
            inconsistencias);

        ValidarRequerido(
            prefijo,
            fila,
            "PREFIJO",
            "PREFIJO_REQUERIDO",
            inconsistencias);

        ValidarRequerido(
            factura,
            fila,
            "FACTURA",
            "FACTURA_REQUERIDA",
            inconsistencias);

        ValidarRequerido(
            aseguradora,
            fila,
            "ASEGURADORA",
            "ASEGURADORA_REQUERIDA",
            inconsistencias);

        ValidarRequerido(
            tipoTexto,
            fila,
            "TIPO NOTA",
            "TIPO_NOTA_REQUERIDO",
            inconsistencias);

        ValidarRequerido(
            numeroNota,
            fila,
            "NUMERO NOTA",
            "NUMERO_NOTA_REQUERIDO",
            inconsistencias);

        ValidarCorrespondenciaFe(
            fila,
            fe,
            prefijo,
            factura,
            inconsistencias);

        ValidarLongitud(
            numeroNota,
            NotaFactura.NumeroLongitudMaxima,
            fila,
            "NUMERO NOTA",
            inconsistencias);

        TipoNotaFactura? tipo = null;

        if (!string.IsNullOrWhiteSpace(tipoTexto))
        {
            if (ConversorTipoNotaFacturaImportacion
                .IntentarConvertir(
                    tipoTexto,
                    out var tipoConvertido))
            {
                tipo = tipoConvertido;
            }
            else
            {
                AgregarError(
                    inconsistencias,
                    fila,
                    "TIPO NOTA",
                    "TIPO_NOTA_INVALIDO",
                    "El tipo debe ser crédito o débito.");
            }
        }

        var fechaNota =
            ObtenerFecha(
                hoja.Cell(
                    fila,
                    columnas["FECHA NOTA"]),
                fila,
                inconsistencias);

        var valorNota =
            ObtenerValor(
                hoja.Cell(
                    fila,
                    columnas["VALOR NOTA"]),
                fila,
                inconsistencias);

        var aseguradoraId =
            ResolverAseguradora(
                aseguradora,
                fila,
                indiceAseguradoras,
                catalogosNoMapeados,
                inconsistencias);

        if (!string.IsNullOrWhiteSpace(fe) &&
            tipo.HasValue &&
            !string.IsNullOrWhiteSpace(numeroNota))
        {
            var clave =
                $"{fe.Trim()}|" +
                $"{(int)tipo.Value}|" +
                $"{numeroNota.Trim()}";

            if (!clavesNotas.Add(clave))
            {
                AgregarError(
                    inconsistencias,
                    fila,
                    "NUMERO NOTA",
                    "NOTA_DUPLICADA_ARCHIVO",
                    "La misma nota aparece más de una vez " +
                    "en el archivo.");
            }
        }

        return new FilaNota(
            fila,
            fe.Trim().ToUpperInvariant(),
            aseguradoraId,
            tipo,
            fechaNota,
            valorNota);
    }

    private static void ValidarReferencias(
        IEnumerable<FilaNota> filas,
        IEnumerable<ReferenciaFacturaImportacionDto>
            referencias,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
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

            if (fila.FechaNota.HasValue &&
                fila.FechaNota.Value <
                factura.FechaFactura)
            {
                AgregarError(
                    inconsistencias,
                    fila.NumeroFila,
                    "FECHA NOTA",
                    "FECHA_NOTA_ANTERIOR_FACTURA",
                    "La fecha de la nota no puede ser " +
                    "anterior a la fecha de factura.");
            }
        }
    }

    private static DateOnly? ObtenerFecha(
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
                "FECHA NOTA",
                "FECHA_NOTA_REQUERIDA",
                "La fecha de la nota es obligatoria.");

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
            "FECHA NOTA",
            "FECHA_NOTA_INVALIDA",
            "El valor no corresponde a una fecha válida.");

        return null;
    }

    private static decimal? ObtenerValor(
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
                "VALOR NOTA",
                "VALOR_NOTA_REQUERIDO",
                "El valor de la nota es obligatorio.");

            return null;
        }

        if (!IntentarObtenerDecimal(
                celda,
                out var valor))
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR NOTA",
                "VALOR_NOTA_INVALIDO",
                "El valor de la nota no es numérico.");

            return null;
        }

        if (valor <= decimal.Zero)
        {
            AgregarError(
                inconsistencias,
                fila,
                "VALOR NOTA",
                "VALOR_NOTA_NO_POSITIVO",
                "El valor debe ser mayor que cero.");
        }

        return valor;
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

        if (noMapeados.Add(normalizado))
        {
            AgregarError(
                inconsistencias,
                fila,
                "ASEGURADORA",
                "CATALOGO_ASEGURADORA_NO_MAPEADO",
                "La aseguradora no existe en el catálogo.");
        }

        return null;
    }

    private static IReadOnlyDictionary<string, int>
        CrearIndiceCatalogo(
            IEnumerable<ReferenciaCatalogoImportacionDto>
                elementos)
    {
        return elementos
            .Where(elemento =>
                !string.IsNullOrWhiteSpace(
                    elemento.Valor))
            .GroupBy(elemento =>
                NormalizadorEncabezadoImportacion
                    .Normalizar(elemento.Valor))
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.First().Id,
                StringComparer.Ordinal);
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

    private static void ValidarRequerido(
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
                "NUMERO_NOTA_LONGITUD_EXCEDIDA",
                $"El valor no puede superar los " +
                $"{longitudMaxima} caracteres.");
        }
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

    private static
        ResultadoValidacionNotasFacturaDto
        CrearResultadoEstructuralInvalido(
            string nombreArchivo,
            ResultadoInspeccionPlantillaDto inspeccion)
    {
        return new ResultadoValidacionNotasFacturaDto
        {
            NombreArchivo = nombreArchivo.Trim(),
            HojasDetectadas = inspeccion.HojasDetectadas,

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
                        .Error
            });
    }

    private sealed record FilaNota(
        int NumeroFila,
        string IdentificadorFe,
        int? AseguradoraId,
        TipoNotaFactura? Tipo,
        DateOnly? FechaNota,
        decimal? ValorNota);
}