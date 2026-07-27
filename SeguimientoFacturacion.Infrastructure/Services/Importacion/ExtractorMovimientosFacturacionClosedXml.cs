using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Detecta y extrae movimientos financieros desde los bloques
/// dinámicos de un archivo de seguimiento de facturación.
/// </summary>
internal static class ExtractorMovimientosFacturacionClosedXml
{
    private const int FilaEncabezadoPrincipal = 1;
    private const int FilaSubencabezado = 2;

    /// <summary>
    /// Detecta las columnas de movimientos disponibles en una hoja.
    /// </summary>
    internal static EsquemaMovimientos Detectar(
        IXLWorksheet hoja,
        int ultimaColumna)
    {
        ArgumentNullException.ThrowIfNull(hoja);

        var notasCredito =
            new List<ColumnasNotaCredito>();

        var abonos =
            new List<ColumnasAbono>();

        for (var columna = 1;
             columna <= ultimaColumna;
             columna++)
        {
            var subencabezado =
                NormalizarTexto(
                    ObtenerTexto(
                        hoja,
                        FilaSubencabezado,
                        columna));

            if (EsEncabezadoNotaCredito(subencabezado))
            {
                if (columna + 2 > ultimaColumna)
                {
                    throw new InvalidOperationException(
                        "El bloque de nota crédito está incompleto.");
                }

                notasCredito.Add(
                    new ColumnasNotaCredito(
                        ObtenerAnioEncabezado(
                            hoja,
                            columna),
                        columna,
                        columna + 1,
                        columna + 2));

                continue;
            }

            if (EsEncabezadoAbono(subencabezado))
            {
                if (columna + 1 > ultimaColumna)
                {
                    throw new InvalidOperationException(
                        "El bloque de abonos está incompleto.");
                }

                abonos.Add(
                    new ColumnasAbono(
                        ObtenerAnioEncabezado(
                            hoja,
                            columna),
                        columna,
                        columna + 1));
            }
        }

        return new EsquemaMovimientos(
            notasCredito,
            abonos,

            BuscarColumnaPrincipal(
                hoja,
                ultimaColumna,
                "FECHADEDEGLOSAYODEVOLUCION",
                "FECHADEGLOSAYODEVOLUCION",
                "FECHAGLOSA"),

            BuscarColumnaPrincipal(
                hoja,
                ultimaColumna,
                "VALORDELAGLOSAYODEVOLUCION",
                "VALORDEGLOSAYODEVOLUCION",
                "VALORGLOSA"),

            BuscarColumnaPrincipal(
                hoja,
                ultimaColumna,
                "VALORCONCILIADO"),

            BuscarColumnaPrincipal(
                hoja,
                ultimaColumna,
                "FECHADECONCILIACION",
                "FECHACONCILIACION"));
    }

    /// <summary>
    /// Extrae todos los movimientos de una fila de factura.
    /// </summary>
    internal static IReadOnlyCollection<
        MovimientoPreparadoImportacionDto> Extraer(
            IXLWorksheet hoja,
            int fila,
            EsquemaMovimientos esquema)
    {
        ArgumentNullException.ThrowIfNull(hoja);
        ArgumentNullException.ThrowIfNull(esquema);

        var movimientos =
            new List<MovimientoPreparadoImportacionDto>();

        ExtraerNotasCredito(
            hoja,
            fila,
            esquema.NotasCredito,
            movimientos);

        ExtraerAbonos(
            hoja,
            fila,
            esquema.Abonos,
            movimientos);

        ExtraerGlosa(
            hoja,
            fila,
            esquema,
            movimientos);

        ExtraerConciliacion(
            hoja,
            fila,
            esquema,
            movimientos);

        return movimientos;
    }

    private static void ExtraerNotasCredito(
        IXLWorksheet hoja,
        int fila,
        IEnumerable<ColumnasNotaCredito> grupos,
        ICollection<MovimientoPreparadoImportacionDto>
            movimientos)
    {
        foreach (var grupo in grupos)
        {
            var numero =
                ObtenerTexto(
                    hoja,
                    fila,
                    grupo.Numero);

            var fecha =
                ObtenerFechaOpcional(
                    hoja,
                    fila,
                    grupo.Fecha,
                    "FECHA DE NOTA CRÉDITO");

            var valor =
                ObtenerDecimalOpcional(
                    hoja,
                    fila,
                    grupo.Valor,
                    "VALOR NOTA CRÉDITO");

            var tieneMovimiento =
                !string.IsNullOrWhiteSpace(numero) ||
                fecha.HasValue ||
                valor is not null &&
                valor.Value != decimal.Zero;

            if (!tieneMovimiento)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(numero))
            {
                throw CrearExcepcionMovimiento(
                    fila,
                    "El número de nota crédito es obligatorio.");
            }

            if (!valor.HasValue)
            {
                throw CrearExcepcionMovimiento(
                    fila,
                    "La nota crédito no contiene un valor.");
            }

            ValidarValorNoNegativo(
                valor.Value,
                fila,
                "nota crédito");

            ValidarFechaDelAnio(
                fecha,
                grupo.Anio,
                fila,
                "nota crédito");

            movimientos.Add(
                new MovimientoPreparadoImportacionDto
                {
                    HojaOrigen = hoja.Name,
                    FilaOrigen = fila,

                    TipoMovimientoId =
                        TipoMovimientoCodigo.NotaCredito,

                    Anio = grupo.Anio,
                    Fecha = fecha,
                    Valor = valor.Value,

                    NumeroNotaCredito =
                        numero.Trim().ToUpperInvariant()
                });
        }
    }

    private static void ExtraerAbonos(
        IXLWorksheet hoja,
        int fila,
        IEnumerable<ColumnasAbono> grupos,
        ICollection<MovimientoPreparadoImportacionDto>
            movimientos)
    {
        foreach (var grupo in grupos)
        {
            var valor =
                ObtenerDecimalOpcional(
                    hoja,
                    fila,
                    grupo.Valor,
                    "ABONOS");

            var fecha =
                ObtenerFechaOpcional(
                    hoja,
                    fila,
                    grupo.Fecha,
                    "FECHA DE ABONO");

            var tieneMovimiento =
                fecha.HasValue ||
                valor is not null &&
                valor.Value != decimal.Zero;

            if (!tieneMovimiento)
            {
                continue;
            }

            if (!valor.HasValue)
            {
                throw CrearExcepcionMovimiento(
                    fila,
                    "El abono no contiene un valor.");
            }

            ValidarValorNoNegativo(
                valor.Value,
                fila,
                "abono");

            ValidarFechaDelAnio(
                fecha,
                grupo.Anio,
                fila,
                "abono");

            movimientos.Add(
                new MovimientoPreparadoImportacionDto
                {
                    HojaOrigen = hoja.Name,
                    FilaOrigen = fila,

                    TipoMovimientoId =
                        TipoMovimientoCodigo.Abono,

                    Anio = grupo.Anio,
                    Fecha = fecha,
                    Valor = valor.Value,
                    NumeroNotaCredito = null,

                    Observacion =
                        fecha.HasValue
                            ? null
                            : "Abono anual importado " +
                              "sin fecha exacta."
                });
        }
    }

    private static void ExtraerGlosa(
        IXLWorksheet hoja,
        int fila,
        EsquemaMovimientos esquema,
        ICollection<MovimientoPreparadoImportacionDto>
            movimientos)
    {
        if (!esquema.FechaGlosa.HasValue ||
            !esquema.ValorGlosa.HasValue)
        {
            return;
        }

        var fecha =
            ObtenerFechaOpcional(
                hoja,
                fila,
                esquema.FechaGlosa.Value,
                "FECHA DE GLOSA Y/O DEVOLUCIÓN");

        var valor =
            ObtenerDecimalOpcional(
                hoja,
                fila,
                esquema.ValorGlosa.Value,
                "VALOR DE GLOSA Y/O DEVOLUCIÓN");

        var tieneMovimiento =
            fecha.HasValue ||
            valor is not null &&
            valor.Value != decimal.Zero;

        if (!tieneMovimiento)
        {
            return;
        }

        if (!fecha.HasValue)
        {
            throw CrearExcepcionMovimiento(
                fila,
                "La glosa o devolución requiere una fecha.");
        }

        if (!valor.HasValue)
        {
            throw CrearExcepcionMovimiento(
                fila,
                "La glosa o devolución requiere un valor.");
        }

        ValidarValorNoNegativo(
            valor.Value,
            fila,
            "glosa o devolución");

        movimientos.Add(
            new MovimientoPreparadoImportacionDto
            {
                HojaOrigen = hoja.Name,
                FilaOrigen = fila,

                TipoMovimientoId =
                    TipoMovimientoCodigo.GlosaODevolucion,

                Anio = fecha.Value.Year,
                Fecha = fecha,
                Valor = valor.Value,
                NumeroNotaCredito = null
            });
    }

    private static void ExtraerConciliacion(
        IXLWorksheet hoja,
        int fila,
        EsquemaMovimientos esquema,
        ICollection<MovimientoPreparadoImportacionDto>
            movimientos)
    {
        if (!esquema.ValorConciliacion.HasValue ||
            !esquema.FechaConciliacion.HasValue)
        {
            return;
        }

        var valor =
            ObtenerDecimalOpcional(
                hoja,
                fila,
                esquema.ValorConciliacion.Value,
                "VALOR CONCILIADO");

        var fecha =
            ObtenerFechaOpcional(
                hoja,
                fila,
                esquema.FechaConciliacion.Value,
                "FECHA CONCILIACIÓN");

        var tieneMovimiento =
            fecha.HasValue ||
            valor is not null &&
            valor.Value != decimal.Zero;

        if (!tieneMovimiento)
        {
            return;
        }

        if (!fecha.HasValue)
        {
            throw CrearExcepcionMovimiento(
                fila,
                "La conciliación requiere una fecha.");
        }

        if (!valor.HasValue)
        {
            throw CrearExcepcionMovimiento(
                fila,
                "La conciliación requiere un valor.");
        }

        ValidarValorNoNegativo(
            valor.Value,
            fila,
            "conciliación");

        movimientos.Add(
            new MovimientoPreparadoImportacionDto
            {
                HojaOrigen = hoja.Name,
                FilaOrigen = fila,

                TipoMovimientoId =
                    TipoMovimientoCodigo.Conciliacion,

                Anio = fecha.Value.Year,
                Fecha = fecha,
                Valor = valor.Value,
                NumeroNotaCredito = null
            });
    }

    private static bool EsEncabezadoNotaCredito(
        string encabezado)
    {
        return encabezado is
            "NODENOTACREDITO" or
            "NUMERODENOTACREDITO" or
            "NOTACREDITO";
    }

    private static bool EsEncabezadoAbono(
        string encabezado)
    {
        return encabezado is "ABONO" or "ABONOS";
    }

    private static int ObtenerAnioEncabezado(
        IXLWorksheet hoja,
        int columna)
    {
        var encabezado =
            ObtenerTexto(
                hoja,
                FilaEncabezadoPrincipal,
                columna);

        var coincidencia =
            Regex.Match(
                encabezado,
                @"\b(20\d{2})\b",
                RegexOptions.CultureInvariant);

        if (coincidencia.Success &&
            int.TryParse(
                coincidencia.Groups[1].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var anio))
        {
            return anio;
        }

        throw new InvalidOperationException(
            $"No se encontró un año válido para el bloque " +
            $"que inicia en la columna {columna}.");
    }

    private static int? BuscarColumnaPrincipal(
        IXLWorksheet hoja,
        int ultimaColumna,
        params string[] encabezadosEsperados)
    {
        for (var columna = 1;
             columna <= ultimaColumna;
             columna++)
        {
            var encabezado =
                NormalizarTexto(
                    ObtenerTexto(
                        hoja,
                        FilaEncabezadoPrincipal,
                        columna));

            if (encabezadosEsperados.Contains(
                    encabezado,
                    StringComparer.Ordinal))
            {
                return columna;
            }
        }

        return null;
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

        if (celda.TryGetValue<DateTime>(
                out var fechaHora))
        {
            return DateOnly.FromDateTime(fechaHora);
        }

        var texto =
            ObtenerTextoCelda(celda);

        if (DateOnly.TryParse(
                texto,
                CultureInfo.GetCultureInfo("es-CO"),
                DateTimeStyles.AllowWhiteSpaces,
                out var fecha))
        {
            return fecha;
        }

        if (DateOnly.TryParse(
                texto,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out fecha))
        {
            return fecha;
        }

        throw new InvalidOperationException(
            $"La fila {fila} contiene una fecha inválida " +
            $"en '{nombreColumna}'.");
    }

    private static decimal? ObtenerDecimalOpcional(
        IXLWorksheet hoja,
        int fila,
        int columna,
        string nombreColumna)
    {
        var celda =
            hoja.Cell(fila, columna);

        var texto =
            ObtenerTextoCelda(celda);

        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (celda.TryGetValue<decimal>(out var valor))
        {
            return valor;
        }

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

    private static void ValidarFechaDelAnio(
        DateOnly? fecha,
        int anio,
        int fila,
        string tipoMovimiento)
    {
        if (fecha.HasValue &&
            fecha.Value.Year != anio)
        {
            throw CrearExcepcionMovimiento(
                fila,
                $"La fecha del movimiento de {tipoMovimiento} " +
                $"no pertenece al año {anio}.");
        }
    }

    private static void ValidarValorNoNegativo(
        decimal valor,
        int fila,
        string tipoMovimiento)
    {
        if (valor < decimal.Zero)
        {
            throw CrearExcepcionMovimiento(
                fila,
                $"El valor del movimiento de {tipoMovimiento} " +
                "no puede ser negativo.");
        }
    }

    private static InvalidOperationException
        CrearExcepcionMovimiento(
            int fila,
            string mensaje)
    {
        return new InvalidOperationException(
            $"No fue posible preparar el movimiento de la " +
            $"fila {fila}. {mensaje}");
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

    internal sealed record EsquemaMovimientos(
        IReadOnlyCollection<ColumnasNotaCredito>
            NotasCredito,

        IReadOnlyCollection<ColumnasAbono>
            Abonos,

        int? FechaGlosa,
        int? ValorGlosa,
        int? ValorConciliacion,
        int? FechaConciliacion);

    internal sealed record ColumnasNotaCredito(
        int Anio,
        int Numero,
        int Fecha,
        int Valor);

    internal sealed record ColumnasAbono(
        int Anio,
        int Valor,
        int Fecha);
}