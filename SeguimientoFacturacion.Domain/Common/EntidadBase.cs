using SeguimientoFacturacion.Domain.Interfaces;

namespace SeguimientoFacturacion.Domain.Common;

/// <summary>
/// Representa la clase base para todas las entidades del dominio.
/// </summary>
/// <typeparam name="TIdentificador">
/// Tipo utilizado para identificar de manera única la entidad.
/// </typeparam>
public abstract class EntidadBase<TIdentificador> :
    IEntidad<TIdentificador>
    where TIdentificador : notnull
{
    /// <summary>
    /// Inicializa una nueva instancia de la entidad.
    /// </summary>
    protected EntidadBase()
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia con su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la entidad.</param>
    protected EntidadBase(TIdentificador id)
    {
        Id = id;
    }

    /// <inheritdoc />
    public TIdentificador Id { get; protected set; } = default!;
}