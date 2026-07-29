using System.Collections.ObjectModel;
using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Inspecciona estructuras de plantillas XLSX mediante
/// ClosedXML.
/// </summary>
public sealed class InspectorEstructuraPlantillaClosedXml :
    IInspectorEstructuraPlantilla
{
    /// <inheritdoc />
    public async Task<ResultadoInspeccionPlantillaDto>
        InspeccionarAsync(
            string nombreArchivo,
            Stream contenido,
            TipoImportacion? tipoEsperado = null,
            CancellationToken cancellationToken = default)
    {
        ValidarNombreArchivo(nombreArchivo);
        ArgumentNullException.ThrowIfNull(contenido);

        if (!contenido.CanRead)
        {
            throw new ArgumentException(
                "El contenido del archivo no puede leerse.",
                nameof(contenido));
        }

        if (tipoEsperado.HasValue)
        {
            /*
             * También valida que el tipo tenga un contrato
             * modular registrado.
             */
            ContratosPlantillasImportacion.Obtener(
                tipoEsperado.Value);
        }

        cancellationToken.ThrowIfCancellationRequested();

        await using var contenidoLocal =
            new MemoryStream();

        var posicionOriginal =
            contenido.CanSeek
                ? contenido.Position
                : (long?)null;

        try
        {
            if (contenido.CanSeek)
            {
                contenido.Position = 0;
            }

            await contenido.CopyToAsync(
                contenidoLocal,
                cancellationToken);
        }
        finally
        {
            if (posicionOriginal.HasValue)
            {
                contenido.Position =
                    posicionOriginal.Value;
            }
        }

        contenidoLocal.Position = 0;

        try
        {
            using var libro =
                new XLWorkbook(contenidoLocal);

            return InspeccionarLibro(
                nombreArchivo.Trim(),
                libro,
                tipoEsperado,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return CrearResultadoArchivoInvalido(
                nombreArchivo.Trim());
        }
    }

    private static ResultadoInspeccionPlantillaDto
        InspeccionarLibro(
            string nombreArchivo,
            XLWorkbook libro,
            TipoImportacion? tipoEsperado,
            CancellationToken cancellationToken)
    {
        var nombresHojas =
            libro.Worksheets
                .Select(hoja => hoja.Name)
                .ToArray();

        var hojasConContenido =
            libro.Worksheets
                .Where(
                    hoja =>
                        hoja.LastCellUsed() is not null)
                .ToArray();

        if (hojasConContenido.Length == 0)
        {
            return CrearResultadoConError(
                nombreArchivo,
                nombresHojas,
                null,
                null,
                0,
                new Dictionary<string, int>(
                    StringComparer.Ordinal),
                fila: 1,
                columna: "ARCHIVO",
                codigo: "PLANTILLA_VACIA",
                mensaje:
                    "El archivo no contiene hojas con datos.");
        }

        if (hojasConContenido.Length > 1)
        {
            return CrearResultadoConError(
                nombreArchivo,
                nombresHojas,
                null,
                null,
                0,
                new Dictionary<string, int>(
                    StringComparer.Ordinal),
                fila: 1,
                columna: "ARCHIVO",
                codigo:
                    "MULTIPLES_HOJAS_CON_DATOS",
                mensaje:
                    "La plantilla modular debe contener una " +
                    "sola hoja con datos.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var hoja = hojasConContenido[0];

        var ultimaColumna =
            hoja.LastColumnUsed()?.ColumnNumber()
            ?? 0;

        var ultimaFila =
            hoja.LastRowUsed()?.RowNumber()
            ?? 0;

        var encabezados =
            LeerEncabezados(
                hoja,
                ultimaColumna);

        var inconsistencias =
            new List<InconsistenciaImportacionDto>();

        ValidarEncabezadosDuplicados(
            encabezados,
            inconsistencias);

        var contratoDetectado =
            ContratosPlantillasImportacion
                .Detectar(
                    encabezados.Select(
                        encabezado =>
                            encabezado.Texto));

        ContratoPlantillaImportacion?
            contratoSeleccionado;

        if (tipoEsperado.HasValue)
        {
            var contratoEsperado =
                ContratosPlantillasImportacion.Obtener(
                    tipoEsperado.Value);

            if (contratoDetectado is not null &&
                contratoDetectado.Tipo !=
                tipoEsperado.Value)
            {
                AgregarError(
                    inconsistencias,
                    fila:
                        ContratosPlantillasImportacion
                            .FilaEncabezados,
                    columna: "ARCHIVO",
                    codigo:
                        "TIPO_PLANTILLA_INCORRECTO",
                    mensaje:
                        $"Se esperaba una plantilla de " +
                        $"'{tipoEsperado.Value}', pero se " +
                        $"detectó '{contratoDetectado.Tipo}'.");

                contratoSeleccionado =
                    contratoDetectado;
            }
            else
            {
                contratoSeleccionado =
                    contratoEsperado;

                AgregarErroresContrato(
                    contratoEsperado,
                    encabezados,
                    inconsistencias);
            }
        }
        else
        {
            contratoSeleccionado =
                contratoDetectado;

            if (contratoSeleccionado is null)
            {
                AgregarError(
                    inconsistencias,
                    fila:
                        ContratosPlantillasImportacion
                            .FilaEncabezados,
                    columna: "ENCABEZADOS",
                    codigo:
                        "PLANTILLA_NO_RECONOCIDA",
                    mensaje:
                        "Los encabezados no corresponden a " +
                        "ninguna plantilla modular registrada.");
            }
        }

        var columnas =
            contratoSeleccionado is null
                ? new Dictionary<string, int>(
                    StringComparer.Ordinal)
                : ResolverColumnas(
                    contratoSeleccionado,
                    encabezados,
                    inconsistencias);

        var tipoDetectado =
            contratoDetectado?.Tipo;

        if (tipoDetectado is null &&
            tipoEsperado.HasValue &&
            inconsistencias.All(
                inconsistencia =>
                    inconsistencia.Severidad !=
                    SeveridadInconsistenciaImportacion
                        .Error))
        {
            tipoDetectado = tipoEsperado.Value;
        }

        return new ResultadoInspeccionPlantillaDto
        {
            NombreArchivo = nombreArchivo,
            HojasDetectadas = nombresHojas,
            NombreHojaDatos = hoja.Name,
            TipoDetectado = tipoDetectado,
            UltimaFilaUtilizada = ultimaFila,

            Columnas =
                new ReadOnlyDictionary<string, int>(
                    columnas),

            Inconsistencias =
                inconsistencias.ToArray()
        };
    }

    private static IReadOnlyCollection<EncabezadoExcel>
        LeerEncabezados(
            IXLWorksheet hoja,
            int ultimaColumna)
    {
        var encabezados =
            new List<EncabezadoExcel>();

        for (var columna = 1;
             columna <= ultimaColumna;
             columna++)
        {
            var texto =
                hoja.Cell(
                        ContratosPlantillasImportacion
                            .FilaEncabezados,
                        columna)
                    .CachedValue
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(texto))
            {
                continue;
            }

            encabezados.Add(
                new EncabezadoExcel(
                    columna,
                    texto));
        }

        return encabezados;
    }

    private static void
        ValidarEncabezadosDuplicados(
            IReadOnlyCollection<EncabezadoExcel>
                encabezados,
            ICollection<InconsistenciaImportacionDto>
                inconsistencias)
    {
        var duplicados =
            encabezados
                .GroupBy(
                    encabezado =>
                        NormalizadorEncabezadoImportacion
                            .Normalizar(
                                encabezado.Texto),
                    StringComparer.Ordinal)
                .Where(
                    grupo =>
                        !string.IsNullOrWhiteSpace(
                            grupo.Key) &&
                        grupo.Count() > 1);

        foreach (var duplicado in duplicados)
        {
            var columnas =
                string.Join(
                    ", ",
                    duplicado.Select(
                        encabezado =>
                            encabezado.Columna));

            AgregarError(
                inconsistencias,
                fila:
                    ContratosPlantillasImportacion
                        .FilaEncabezados,
                columna:
                    duplicado.First().Texto,
                codigo:
                    "ENCABEZADO_DUPLICADO",
                mensaje:
                    $"El encabezado aparece repetido en " +
                    $"las columnas {columnas}.");
        }
    }

    private static void AgregarErroresContrato(
        ContratoPlantillaImportacion contrato,
        IReadOnlyCollection<EncabezadoExcel>
            encabezados,
        ICollection<InconsistenciaImportacionDto>
            inconsistencias)
    {
        var textos =
            encabezados
                .Select(
                    encabezado =>
                        encabezado.Texto)
                .ToArray();

        var faltantes =
            contrato.ObtenerEncabezadosFaltantes(
                textos);

        foreach (var faltante in faltantes)
        {
            AgregarError(
                inconsistencias,
                fila:
                    ContratosPlantillasImportacion
                        .FilaEncabezados,
                columna: faltante,
                codigo:
                    "ENCABEZADO_REQUERIDO_AUSENTE",
                mensaje:
                    "No se encontró el encabezado " +
                    "obligatorio.");
        }

        var noReconocidos =
            contrato.ObtenerEncabezadosNoReconocidos(
                textos);

        foreach (var noReconocido in noReconocidos)
        {
            AgregarError(
                inconsistencias,
                fila:
                    ContratosPlantillasImportacion
                        .FilaEncabezados,
                columna: noReconocido,
                codigo:
                    "ENCABEZADO_NO_PERMITIDO",
                mensaje:
                    "El encabezado no pertenece a la " +
                    "plantilla modular seleccionada.");
        }
    }

    private static Dictionary<string, int>
        ResolverColumnas(
            ContratoPlantillaImportacion contrato,
            IReadOnlyCollection<EncabezadoExcel>
                encabezados,
            ICollection<InconsistenciaImportacionDto>
                inconsistencias)
    {
        var columnas =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        foreach (var encabezado in encabezados)
        {
            var nombreCanonico =
                contrato.ResolverEncabezado(
                    encabezado.Texto);

            if (nombreCanonico is null)
            {
                continue;
            }

            if (!columnas.TryAdd(
                    nombreCanonico,
                    encabezado.Columna))
            {
                AgregarError(
                    inconsistencias,
                    fila:
                        ContratosPlantillasImportacion
                            .FilaEncabezados,
                    columna: encabezado.Texto,
                    codigo:
                        "ENCABEZADO_DUPLICADO",
                    mensaje:
                        "Dos columnas representan el mismo " +
                        "encabezado canónico.");
            }
        }

        return columnas;
    }

    private static ResultadoInspeccionPlantillaDto
        CrearResultadoArchivoInvalido(
            string nombreArchivo)
    {
        return CrearResultadoConError(
            nombreArchivo,
            Array.Empty<string>(),
            null,
            null,
            0,
            new Dictionary<string, int>(
                StringComparer.Ordinal),
            fila: null,
            columna: "ARCHIVO",
            codigo: "ARCHIVO_XLSX_INVALIDO",
            mensaje:
                "El archivo no tiene una estructura XLSX " +
                "válida o no pudo ser leído.");
    }

    private static ResultadoInspeccionPlantillaDto
        CrearResultadoConError(
            string nombreArchivo,
            IReadOnlyCollection<string> hojas,
            string? nombreHojaDatos,
            TipoImportacion? tipoDetectado,
            int ultimaFila,
            Dictionary<string, int> columnas,
            int? fila,
            string columna,
            string codigo,
            string mensaje)
    {
        return new ResultadoInspeccionPlantillaDto
        {
            NombreArchivo = nombreArchivo,
            HojasDetectadas = hojas,
            NombreHojaDatos = nombreHojaDatos,
            TipoDetectado = tipoDetectado,
            UltimaFilaUtilizada = ultimaFila,

            Columnas =
                new ReadOnlyDictionary<string, int>(
                    columnas),

            Inconsistencias =
            [
                new InconsistenciaImportacionDto
                {
                    Fila = fila,
                    Columna = columna,
                    Codigo = codigo,
                    Mensaje = mensaje,
                    Severidad =
                        SeveridadInconsistenciaImportacion
                            .Error
                }
            ]
        };
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

    private static void ValidarNombreArchivo(
        string nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.",
                nameof(nombreArchivo));
        }
    }

    private sealed record EncabezadoExcel(
        int Columna,
        string Texto);
}