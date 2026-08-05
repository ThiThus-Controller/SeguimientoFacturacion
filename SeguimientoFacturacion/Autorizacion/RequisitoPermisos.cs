using Microsoft.AspNetCore.Authorization;
using SeguimientoFacturacion.Domain.Constants;

namespace SeguimientoFacturacion.Autorizacion;

/// <summary>
/// Exige que el usuario cumpla todos los permisos de al menos
/// una de las alternativas configuradas.
/// </summary>
public sealed class RequisitoPermisos : IAuthorizationRequirement
{
    /// <summary>
    /// Inicializa el requisito con una o más alternativas válidas.
    /// </summary>
    public RequisitoPermisos(
        IEnumerable<IEnumerable<string>> alternativas)
    {
        ArgumentNullException.ThrowIfNull(alternativas);

        var normalizadas = alternativas
            .Select(
                alternativa => NormalizarAlternativa(
                    alternativa))
            .ToArray();

        if (normalizadas.Length == 0)
        {
            throw new ArgumentException(
                "Debe configurarse al menos una alternativa de permisos.",
                nameof(alternativas));
        }

        Alternativas = normalizadas;
    }

    /// <summary>
    /// Obtiene los conjuntos alternativos de permisos. Todos los
    /// permisos de una alternativa deben estar concedidos.
    /// </summary>
    public IReadOnlyCollection<IReadOnlySet<string>> Alternativas
    {
        get;
    }

    /// <summary>
    /// Crea un requisito que exige todos los permisos indicados.
    /// </summary>
    public static RequisitoPermisos ExigirTodos(
        params string[] permisos)
    {
        ArgumentNullException.ThrowIfNull(permisos);

        return new RequisitoPermisos(
            new[] { permisos.AsEnumerable() });
    }

    private static IReadOnlySet<string> NormalizarAlternativa(
        IEnumerable<string> alternativa)
    {
        ArgumentNullException.ThrowIfNull(alternativa);

        var permisos = alternativa
            .Select(PermisosSistema.Normalizar)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permisos.Count == 0)
        {
            throw new ArgumentException(
                "Una alternativa de autorización no puede estar vacía.",
                nameof(alternativa));
        }

        return permisos;
    }
}
