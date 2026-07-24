using FluentValidation.Results;

namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa los errores encontrados durante la validación
/// de una solicitud de la capa Application.
/// </summary>
public sealed class ExcepcionValidacionAplicacion : Exception
{
    /// <summary>
    /// Inicializa una excepción con los errores de validación encontrados.
    /// </summary>
    /// <param name="fallos">
    /// Colección de fallos generados por FluentValidation.
    /// </param>
    public ExcepcionValidacionAplicacion(
        IEnumerable<ValidationFailure> fallos)
        : base("Uno o más datos de la solicitud no son válidos.")
    {
        ArgumentNullException.ThrowIfNull(fallos);

        Errores = fallos
            .GroupBy(fallo =>
                string.IsNullOrWhiteSpace(fallo.PropertyName)
                    ? "General"
                    : fallo.PropertyName)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo
                    .Select(fallo => fallo.ErrorMessage)
                    .Where(mensaje =>
                        !string.IsNullOrWhiteSpace(mensaje))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray());
    }

    /// <summary>
    /// Obtiene los mensajes agrupados por propiedad.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errores { get; }
}