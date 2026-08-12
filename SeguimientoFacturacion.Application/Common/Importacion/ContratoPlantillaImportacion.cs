using System.Collections.ObjectModel;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Importacion;

/// <summary>
/// Define la estructura autorizada de una plantilla
/// de importación masiva.
/// </summary>
public sealed class ContratoPlantillaImportacion
{
    private readonly
        IReadOnlyDictionary<string, string>
        _equivalenciasEncabezados;

    /// <summary>
    /// Inicializa un contrato de plantilla.
    /// </summary>
    internal ContratoPlantillaImportacion(
        TipoImportacion tipo,
        string nombreHojaSugerido,
        IEnumerable<string> encabezadosRequeridos,
        IReadOnlyDictionary<string, string>?
            aliases = null)
    {
        if (string.IsNullOrWhiteSpace(
                nombreHojaSugerido))
        {
            throw new ArgumentException(
                "El nombre sugerido de la hoja " +
                "es obligatorio.",
                nameof(nombreHojaSugerido));
        }

        ArgumentNullException.ThrowIfNull(
            encabezadosRequeridos);

        var encabezados =
            encabezadosRequeridos
                .Select(
                    encabezado =>
                        encabezado?.Trim())
                .Where(
                    encabezado =>
                        !string.IsNullOrWhiteSpace(
                            encabezado))
                .Cast<string>()
                .ToArray();

        if (encabezados.Length == 0)
        {
            throw new ArgumentException(
                "El contrato debe tener al menos " +
                "un encabezado requerido.",
                nameof(encabezadosRequeridos));
        }

        var equivalencias =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (var encabezado in encabezados)
        {
            var normalizado =
                NormalizadorEncabezadoImportacion
                    .Normalizar(encabezado);

            if (!equivalencias.TryAdd(
                    normalizado,
                    encabezado))
            {
                throw new ArgumentException(
                    $"El encabezado '{encabezado}' está " +
                    "duplicado después de normalizarse.",
                    nameof(encabezadosRequeridos));
            }
        }

        if (aliases is not null)
        {
            foreach (var alias in aliases)
            {
                var nombreCanonico =
                    encabezados.SingleOrDefault(
                        encabezado =>
                            string.Equals(
                                encabezado,
                                alias.Value,
                                StringComparison
                                    .OrdinalIgnoreCase));

                if (nombreCanonico is null)
                {
                    throw new ArgumentException(
                        $"El alias '{alias.Key}' apunta a un " +
                        "encabezado que no pertenece al contrato.",
                        nameof(aliases));
                }

                var aliasNormalizado =
                    NormalizadorEncabezadoImportacion
                        .Normalizar(alias.Key);

                if (string.IsNullOrWhiteSpace(
                        aliasNormalizado))
                {
                    throw new ArgumentException(
                        "El alias no puede estar vacío.",
                        nameof(aliases));
                }

                if (equivalencias.TryGetValue(
                        aliasNormalizado,
                        out var encabezadoExistente) &&
                    !string.Equals(
                        encabezadoExistente,
                        nombreCanonico,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"El alias '{alias.Key}' entra en " +
                        "conflicto con otro encabezado.",
                        nameof(aliases));
                }

                equivalencias[aliasNormalizado] =
                    nombreCanonico;
            }
        }

        Tipo = tipo;

        NombreHojaSugerido =
            nombreHojaSugerido
                .Trim()
                .ToUpperInvariant();

        EncabezadosRequeridos =
            Array.AsReadOnly(encabezados);

        _equivalenciasEncabezados =
            new ReadOnlyDictionary<string, string>(
                equivalencias);
    }

    /// <summary>
    /// Obtiene el tipo de importación.
    /// </summary>
    public TipoImportacion Tipo { get; }

    /// <summary>
    /// Obtiene el nombre recomendado para la hoja.
    /// El lector no dependerá de este nombre.
    /// </summary>
    public string NombreHojaSugerido { get; }

    /// <summary>
    /// Obtiene la lista ordenada de encabezados requeridos.
    /// </summary>
    public IReadOnlyList<string>
        EncabezadosRequeridos
    {
        get;
    }

    /// <summary>
    /// Resuelve el nombre canónico de un encabezado.
    /// </summary>
    /// <param name="encabezado">
    /// Encabezado leído desde el archivo.
    /// </param>
    /// <returns>
    /// Nombre canónico o null cuando no pertenece
    /// al contrato.
    /// </returns>
    public string? ResolverEncabezado(
        string? encabezado)
    {
        var normalizado =
            NormalizadorEncabezadoImportacion
                .Normalizar(encabezado);

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return null;
        }

        return _equivalenciasEncabezados.TryGetValue(
            normalizado,
            out var nombreCanonico)
                ? nombreCanonico
                : null;
    }

    /// <summary>
    /// Obtiene los encabezados obligatorios que no aparecen
    /// en el conjunto recibido.
    /// </summary>
    public IReadOnlyList<string>
        ObtenerEncabezadosFaltantes(
            IEnumerable<string?> encabezados)
    {
        ArgumentNullException.ThrowIfNull(encabezados);

        var encabezadosResueltos =
            encabezados
                .Select(ResolverEncabezado)
                .Where(
                    encabezado =>
                        encabezado is not null)
                .Cast<string>()
                .ToHashSet(
                    StringComparer.Ordinal);

        return EncabezadosRequeridos
            .Where(
                encabezado =>
                    !encabezadosResueltos.Contains(
                        encabezado))
            .ToArray();
    }

    /// <summary>
    /// Obtiene los encabezados que no pertenecen
    /// al contrato.
    /// </summary>
    public IReadOnlyList<string>
        ObtenerEncabezadosNoReconocidos(
            IEnumerable<string?> encabezados)
    {
        ArgumentNullException.ThrowIfNull(encabezados);

        return encabezados
            .Where(
                encabezado =>
                    !string.IsNullOrWhiteSpace(
                        encabezado))
            .Where(
                encabezado =>
                    ResolverEncabezado(encabezado)
                    is null)
            .Select(
                encabezado =>
                    encabezado!.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Determina si los encabezados coinciden exactamente
    /// con el contrato.
    /// </summary>
    public bool CoincideCon(
        IEnumerable<string?> encabezados)
    {
        ArgumentNullException.ThrowIfNull(encabezados);

        var encabezadosMaterializados =
            encabezados.ToArray();

        return ObtenerEncabezadosFaltantes(
                   encabezadosMaterializados)
               .Count == 0
               &&
               ObtenerEncabezadosNoReconocidos(
                   encabezadosMaterializados)
               .Count == 0;
    }
}
