namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa un conflicto de concurrencia detectado
/// durante la persistencia de información.
/// </summary>
public sealed class ExcepcionConcurrenciaPersistencia :
    Exception
{
    /// <summary>
    /// Inicializa la excepción de concurrencia.
    /// </summary>
    public ExcepcionConcurrenciaPersistencia(
        IReadOnlyCollection<string> entidades,
        Exception innerException)
        : base(
            ConstruirMensaje(entidades),
            innerException)
    {
        ArgumentNullException.ThrowIfNull(entidades);
        ArgumentNullException.ThrowIfNull(innerException);

        Entidades =
            entidades
                .Where(
                    entidad =>
                        !string.IsNullOrWhiteSpace(entidad))
                .Select(entidad => entidad.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(
                    entidad => entidad,
                    StringComparer.Ordinal)
                .ToArray();
    }

    /// <summary>
    /// Obtiene los nombres de las entidades que
    /// presentaron el conflicto.
    /// </summary>
    public IReadOnlyList<string> Entidades { get; }

    private static string ConstruirMensaje(
        IReadOnlyCollection<string> entidades)
    {
        ArgumentNullException.ThrowIfNull(entidades);

        var nombres =
            entidades
                .Where(
                    entidad =>
                        !string.IsNullOrWhiteSpace(entidad))
                .Select(entidad => entidad.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(
                    entidad => entidad,
                    StringComparer.Ordinal)
                .ToArray();

        var detalle =
            nombres.Length > 0
                ? string.Join(", ", nombres)
                : "entidad no identificada";

        return
            "La información fue modificada por otra " +
            "operación antes de confirmar los cambios. " +
            $"Entidades involucradas: {detalle}. " +
            "Actualice la información e intente nuevamente.";
    }
}