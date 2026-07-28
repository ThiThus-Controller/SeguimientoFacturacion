using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Valida los movimientos financieros contenidos en un archivo
/// antes de permitir su preparación o persistencia.
/// </summary>
public static class
    ValidadorMovimientosArchivoFacturacionClosedXml
{
    private const int FilaEncabezados = 1;
    private const int PrimeraFilaDatos = 3;

    /// <summary>
    /// Valida los bloques de movimientos de todas las hojas
    /// de facturación encontradas en el libro.
    /// </summary>
    public static IReadOnlyCollection<
        InconsistenciaImportacionDto> Validar(
            XLWorkbook libro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(libro);

        var inconsistencias =
            new List<InconsistenciaImportacionDto>();

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

            var columnasFactura =
                DetectarColumnasFactura(
                    hoja,
                    ultimaColumna);

            if (columnasFactura is null)
            {
                continue;
            }

            ExtractorMovimientosFacturacionClosedXml
                .EsquemaMovimientos esquema;

            try
            {
                esquema =
                    ExtractorMovimientosFacturacionClosedXml
                        .Detectar(
                            hoja,
                            ultimaColumna);
            }
            catch (InvalidOperationException excepcion)
            {
                inconsistencias.Add(
                    CrearInconsistencia(
                        FilaEncabezados,
                        "ENCABEZADOS DE MOVIMIENTOS",
                        "ENCABEZADO_MOVIMIENTO_INVALIDO",
                        excepcion.Message));

                continue;
            }

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
                        columnasFactura))
                {
                    continue;
                }

                try
                {
                    _ =
                        ExtractorMovimientosFacturacionClosedXml
                            .Extraer(
                                hoja,
                                fila,
                                esquema);
                }
                catch (InvalidOperationException excepcion)
                {
                    inconsistencias.Add(
                        CrearInconsistencia(
                            fila,
                            "MOVIMIENTOS",
                            "MOVIMIENTO_INVALIDO",
                            excepcion.Message));
                }
            }
        }

        return inconsistencias;
    }

    private static ColumnasIdentificacionFactura?
        DetectarColumnasFactura(
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
                    ObtenerTexto(
                        hoja,
                        FilaEncabezados,
                        columna));

            if (!string.IsNullOrWhiteSpace(
                    encabezado))
            {
                encabezados.TryAdd(
                    encabezado,
                    columna);
            }
        }

        if (!encabezados.TryGetValue(
                "FE",
                out var columnaFe) ||
            !encabezados.TryGetValue(
                "PREFIJO",
                out var columnaPrefijo) ||
            !encabezados.TryGetValue(
                "FACTURA",
                out var columnaFactura) ||
            !encabezados.ContainsKey("VALOR"))
        {
            return null;
        }

        return new ColumnasIdentificacionFactura(
            columnaFe,
            columnaPrefijo,
            columnaFactura);
    }

    private static bool EsFilaFactura(
        IXLWorksheet hoja,
        int fila,
        ColumnasIdentificacionFactura columnas)
    {
        return TieneContenido(
                   hoja,
                   fila,
                   columnas.Fe) ||
               TieneContenido(
                   hoja,
                   fila,
                   columnas.Prefijo) ||
               TieneContenido(
                   hoja,
                   fila,
                   columnas.Factura);
    }

    private static bool TieneContenido(
        IXLWorksheet hoja,
        int fila,
        int columna)
    {
        return !string.IsNullOrWhiteSpace(
            ObtenerTexto(
                hoja,
                fila,
                columna));
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

    private static InconsistenciaImportacionDto
        CrearInconsistencia(
            int fila,
            string columna,
            string codigo,
            string mensaje)
    {
        return new InconsistenciaImportacionDto
        {
            Fila = fila,
            Columna = columna,
            Codigo = codigo,
            Mensaje = mensaje,

            Severidad =
                SeveridadInconsistenciaImportacion.Error
        };
    }

    private sealed record ColumnasIdentificacionFactura(
        int Fe,
        int Prefijo,
        int Factura);
}