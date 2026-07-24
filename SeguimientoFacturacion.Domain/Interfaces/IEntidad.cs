namespace SeguimientoFacturacion.Domain.Interfaces;

/// <summary>
/// Define el contrato básico que debe cumplir una entidad del dominio.
/// </summary>
/// <typeparam name="TIdentificador">
/// Tipo utilizado para identificar de manera única la entidad.
/// </typeparam>
public interface IEntidad<out TIdentificador>
    where TIdentificador : notnull
{
    /// <summary>
    /// Obtiene el identificador único de la entidad.
    /// </summary>
    TIdentificador Id { get; }
}