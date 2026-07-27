using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Analiza archivos de seguimiento de facturación en formato XLSX
/// utilizando ClosedXML.
/// </summary>
public sealed partial class LectorArchivoFacturacionClosedXml
    : ILectorArchivoFacturacion
{
    private const int FilaEncabezadoPrincipal = 1;
    private const int FilaSubencabezado = 2;
    private const int PrimeraFilaDatos = 3;

    /// <inheritdoc />
    public async Task<ResultadoAnalisisImportacionDto> AnalizarAsync(
        SolicitudAnalisisImportacionDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        cancellationToken.ThrowIfCancellationRequested();

        await using var contenidoLocal = new MemoryStream();

        if (solicitud.Contenido.CanSeek)
        {
            solicitud.Contenido.Position = 0;
        }

        await solicitud.Contenido.CopyToAsync(
            contenidoLocal,
            cancellationToken);

        contenidoLocal.Position = 0;

        using var libro = new XLWorkbook(contenidoLocal);

        var hojasDetectadas = libro.Worksheets
            .Select(hoja => hoja.Name)
            .ToArray();

        var inconsistencias =
            new List<InconsistenciaImportacionDto>();

        var aniosDetectados = new SortedSet<int>();

        var totalFilasAnalizadas = 0;
        var facturasDetectadas = 0;
        var movimientosDetectados = 0;
        var encontroHojaFacturacion = false;

        foreach (var hoja in libro.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ultimaColumna =
                hoja.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (ultimaColumna == 0)
            {
                continue;
            }

            var encabezadosPrincipales = ObtenerColumnasPorEncabezado(
                hoja,
                FilaEncabezadoPrincipal,
                ultimaColumna);

            if (!EsHojaFacturacion(encabezadosPrincipales))
            {
                continue;
            }

            encontroHojaFacturacion = true;

            var resultadoHoja = AnalizarHoja(
                hoja,
                encabezadosPrincipales,
                ultimaColumna,
                cancellationToken);

            totalFilasAnalizadas +=
                resultadoHoja.TotalFilasAnalizadas;

            facturasDetectadas +=
                resultadoHoja.FacturasDetectadas;

            movimientosDetectados +=
                resultadoHoja.MovimientosDetectados;

            foreach (var anio in resultadoHoja.AniosDetectados)
            {
                aniosDetectados.Add(anio);
            }
        }

        if (!encontroHojaFacturacion)
        {
            inconsistencias.Add(
                new InconsistenciaImportacionDto
                {
                    Fila = 0,
                    Columna = "ARCHIVO",
                    Codigo = "HOJA_FACTURACION_NO_ENCONTRADA",
                    Mensaje =
                        "No se encontró una hoja con los encabezados " +
                        "mínimos FE, PREFIJO, FACTURA y VALOR.",
                    Severidad =
                        SeveridadInconsistenciaImportacion.Error
                });
        }
        else if (aniosDetectados.Count == 0)
        {
            inconsistencias.Add(
                new InconsistenciaImportacionDto
                {
                    Fila = 1,
                    Columna = "ENCABEZADOS",
                    Codigo = "ANIO_NO_DETECTADO",
                    Mensaje =
                        "No se detectó ningún año en los encabezados " +
                        "de movimientos del archivo.",
                    Severidad =
                        SeveridadInconsistenciaImportacion.Advertencia
                });
        }

        return new ResultadoAnalisisImportacionDto
        {
            NombreArchivo = solicitud.NombreArchivo,
            HojasDetectadas = hojasDetectadas,
            AniosDetectados = aniosDetectados.ToArray(),
            TotalFilasAnalizadas = totalFilasAnalizadas,
            FacturasDetectadas = facturasDetectadas,
            MovimientosDetectados = movimientosDetectados,
            CatalogosNoMapeados = 0,
            Inconsistencias = inconsistencias
        };
    }

    private static ResultadoHoja AnalizarHoja(
        IXLWorksheet hoja,
        IReadOnlyDictionary<string, IReadOnlyList<int>>
            encabezadosPrincipales,
        int ultimaColumna,
        CancellationToken cancellationToken)
    {
        var encabezadosSecundarios = ObtenerColumnasPorEncabezado(
            hoja,
            FilaSubencabezado,
            ultimaColumna);

        var ultimaFila =
            hoja.LastRowUsed()?.RowNumber() ?? 0;

        if (ultimaFila < PrimeraFilaDatos)
        {
            return ResultadoHoja.Vacio;
        }

        var columnaFe = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "FE");

        var columnaPrefijo = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "PREFIJO");

        var columnaFactura = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "FACTURA");

        var columnasNotaCredito = ObtenerColumnas(
            encabezadosSecundarios,
            "NODENOTACREDITO",
            "NUMERODENOTACREDITO",
            "NOTACREDITO");

        var columnasAbonos = ObtenerColumnas(
            encabezadosSecundarios,
            "ABONOS",
            "ABONO");

        var columnaFechaGlosa = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "FECHADEDEGLOSAYODEVOLUCION",
            "FECHADEGLOSAYODEVOLUCION",
            "FECHAGLOSA");

        var columnaValorGlosa = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "VALORDELAGLOSAYODEVOLUCION",
            "VALORDEGLOSAYODEVOLUCION",
            "VALORGLOSA");

        var columnaConciliacion = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "CONCILIACION");

        var columnaValorConciliado = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "VALORCONCILIADO");

        var columnaFechaConciliacion = ObtenerPrimeraColumna(
            encabezadosPrincipales,
            "FECHADECONCILIACION",
            "FECHACONCILIACION");

        var anios = DetectarAnios(
            hoja,
            ultimaColumna);

        var totalFilas = 0;
        var totalFacturas = 0;
        var totalMovimientos = 0;

        for (var numeroFila = PrimeraFilaDatos;
             numeroFila <= ultimaFila;
             numeroFila++)
        {
            if (numeroFila % 256 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            totalFilas++;

            if (!EsFilaFactura(
                    hoja,
                    numeroFila,
                    columnaFe,
                    columnaPrefijo,
                    columnaFactura))
            {
                continue;
            }

            totalFacturas++;

            totalMovimientos += ContarNotasCredito(
                hoja,
                numeroFila,
                columnasNotaCredito,
                ultimaColumna);

            totalMovimientos += ContarAbonos(
                hoja,
                numeroFila,
                columnasAbonos,
                ultimaColumna);

            if (ExisteMovimientoEnColumnas(
                    hoja,
                    numeroFila,
                    columnaFechaGlosa,
                    columnaValorGlosa))
            {
                totalMovimientos++;
            }

            if (ExisteMovimientoEnColumnas(
                    hoja,
                    numeroFila,
                    columnaConciliacion,
                    columnaValorConciliado,
                    columnaFechaConciliacion))
            {
                totalMovimientos++;
            }
        }

        return new ResultadoHoja(
            totalFilas,
            totalFacturas,
            totalMovimientos,
            anios);
    }

    private static bool EsHojaFacturacion(
        IReadOnlyDictionary<string, IReadOnlyList<int>> encabezados)
    {
        return encabezados.ContainsKey("FE") &&
               encabezados.ContainsKey("PREFIJO") &&
               encabezados.ContainsKey("FACTURA") &&
               encabezados.ContainsKey("VALOR");
    }

    private static bool EsFilaFactura(
        IXLWorksheet hoja,
        int fila,
        int? columnaFe,
        int? columnaPrefijo,
        int? columnaFactura)
    {
        return TieneContenido(hoja, fila, columnaFe) ||
               TieneContenido(hoja, fila, columnaPrefijo) ||
               TieneContenido(hoja, fila, columnaFactura);
    }

    private static int ContarNotasCredito(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyCollection<int> columnasNotaCredito,
        int ultimaColumna)
    {
        var total = 0;

        foreach (var columna in columnasNotaCredito)
        {
            var columnasGrupo = new[]
            {
                columna,
                columna + 1,
                columna + 2
            };

            if (ExisteMovimientoEnColumnas(
                    hoja,
                    fila,
                    columnasGrupo
                        .Where(numero => numero <= ultimaColumna)
                        .Select(numero => (int?)numero)
                        .ToArray()))
            {
                total++;
            }
        }

        return total;
    }

    private static int ContarAbonos(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyCollection<int> columnasAbonos,
        int ultimaColumna)
    {
        var total = 0;

        foreach (var columna in columnasAbonos)
        {
            var columnasGrupo = new[]
            {
                columna,
                columna + 1
            };

            if (ExisteMovimientoEnColumnas(
                    hoja,
                    fila,
                    columnasGrupo
                        .Where(numero => numero <= ultimaColumna)
                        .Select(numero => (int?)numero)
                        .ToArray()))
            {
                total++;
            }
        }

        return total;
    }

    private static bool ExisteMovimientoEnColumnas(
        IXLWorksheet hoja,
        int fila,
        params int?[] columnas)
    {
        return columnas
            .Where(columna => columna.HasValue)
            .Select(columna => columna!.Value)
            .Any(columna =>
                TieneValorSignificativo(
                    hoja.Cell(fila, columna)));
    }

    private static bool TieneContenido(
        IXLWorksheet hoja,
        int fila,
        int? columna)
    {
        if (!columna.HasValue)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(
            ObtenerTextoCelda(
                hoja.Cell(fila, columna.Value)));
    }

    private static bool TieneValorSignificativo(
        IXLCell celda)
    {
        var texto = ObtenerTextoCelda(celda);

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        if (decimal.TryParse(
                texto,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var valorInvariante))
        {
            return valorInvariante != decimal.Zero;
        }

        if (decimal.TryParse(
                texto,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out var valorLocal))
        {
            return valorLocal != decimal.Zero;
        }

        return true;
    }

    private static string ObtenerTextoCelda(
        IXLCell celda)
    {
        /*
         * CachedValue evita forzar el recálculo de fórmulas.
         * Esto es importante para los archivos históricos que
         * contienen fórmulas con vínculos hacia otros archivos.
         */
        return celda.CachedValue
            .ToString()
            .Trim();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<int>>
        ObtenerColumnasPorEncabezado(
            IXLWorksheet hoja,
            int fila,
            int ultimaColumna)
    {
        var columnas =
            new Dictionary<string, List<int>>(
                StringComparer.Ordinal);

        for (var columna = 1;
             columna <= ultimaColumna;
             columna++)
        {
            var encabezado = NormalizarTexto(
                ObtenerTextoCelda(
                    hoja.Cell(fila, columna)));

            if (string.IsNullOrWhiteSpace(encabezado))
            {
                continue;
            }

            if (!columnas.TryGetValue(
                    encabezado,
                    out var posiciones))
            {
                posiciones = [];
                columnas.Add(encabezado, posiciones);
            }

            posiciones.Add(columna);
        }

        return columnas.ToDictionary(
            elemento => elemento.Key,
            elemento =>
                (IReadOnlyList<int>)elemento.Value.AsReadOnly(),
            StringComparer.Ordinal);
    }

    private static int? ObtenerPrimeraColumna(
        IReadOnlyDictionary<string, IReadOnlyList<int>> encabezados,
        params string[] nombres)
    {
        foreach (var nombre in nombres)
        {
            if (encabezados.TryGetValue(
                    nombre,
                    out var columnas) &&
                columnas.Count > 0)
            {
                return columnas[0];
            }
        }

        return null;
    }

    private static IReadOnlyCollection<int> ObtenerColumnas(
        IReadOnlyDictionary<string, IReadOnlyList<int>> encabezados,
        params string[] nombres)
    {
        var columnas = new SortedSet<int>();

        foreach (var nombre in nombres)
        {
            if (!encabezados.TryGetValue(
                    nombre,
                    out var posiciones))
            {
                continue;
            }

            foreach (var posicion in posiciones)
            {
                columnas.Add(posicion);
            }
        }

        return columnas;
    }

    private static IReadOnlyCollection<int> DetectarAnios(
        IXLWorksheet hoja,
        int ultimaColumna)
    {
        var anios = new SortedSet<int>();

        for (var fila = FilaEncabezadoPrincipal;
             fila <= FilaSubencabezado;
             fila++)
        {
            for (var columna = 1;
                 columna <= ultimaColumna;
                 columna++)
            {
                var contenido = ObtenerTextoCelda(
                    hoja.Cell(fila, columna));

                foreach (Match coincidencia
                         in PatronAnio().Matches(contenido))
                {
                    if (int.TryParse(
                            coincidencia.Groups[1].Value,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out var anio))
                    {
                        anios.Add(anio);
                    }
                }
            }
        }

        return anios;
    }

    private static string NormalizarTexto(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var textoDescompuesto = texto
            .Trim()
            .Normalize(NormalizationForm.FormD);

        var resultado = new StringBuilder(
            textoDescompuesto.Length);

        foreach (var caracter in textoDescompuesto)
        {
            var categoria =
                CharUnicodeInfo.GetUnicodeCategory(caracter);

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

        return resultado
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(
        @"\b(20\d{2})\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronAnio();

    private sealed record ResultadoHoja(
        int TotalFilasAnalizadas,
        int FacturasDetectadas,
        int MovimientosDetectados,
        IReadOnlyCollection<int> AniosDetectados)
    {
        public static ResultadoHoja Vacio { get; } =
            new(
                0,
                0,
                0,
                Array.Empty<int>());
    }
}