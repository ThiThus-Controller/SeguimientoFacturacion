namespace SeguimientoFacturacion.Domain.Interfaces;

/// <summary>
/// Define la información y las operaciones necesarias para auditar una entidad.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Obtiene la fecha y hora UTC en la que fue creada la entidad.
    /// </summary>
    DateTimeOffset FechaCreacionUtc { get; }

    /// <summary>
    /// Obtiene el nombre o identificador del usuario que creó la entidad.
    /// </summary>
    string CreadoPor { get; }

    /// <summary>
    /// Obtiene la fecha y hora UTC de la última modificación.
    /// Será nula cuando la entidad no haya sido modificada.
    /// </summary>
    DateTimeOffset? FechaModificacionUtc { get; }

    /// <summary>
    /// Obtiene el nombre o identificador del usuario que realizó
    /// la última modificación.
    /// </summary>
    string? ModificadoPor { get; }

    /// <summary>
    /// Registra la información de creación de la entidad.
    /// </summary>
    /// <param name="fechaCreacion">
    /// Fecha y hora en la que fue creada la entidad.
    /// </param>
    /// <param name="creadoPor">
    /// Nombre o identificador del usuario que creó la entidad.
    /// </param>
    void RegistrarCreacion(
        DateTimeOffset fechaCreacion,
        string creadoPor);

    /// <summary>
    /// Registra la información de la última modificación de la entidad.
    /// </summary>
    /// <param name="fechaModificacion">
    /// Fecha y hora en la que fue modificada la entidad.
    /// </param>
    /// <param name="modificadoPor">
    /// Nombre o identificador del usuario que modificó la entidad.
    /// </param>
    void RegistrarModificacion(
        DateTimeOffset fechaModificacion,
        string modificadoPor);
}