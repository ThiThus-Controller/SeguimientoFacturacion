namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Ejecuta una operación con aislamiento serializable.
/// </summary>
public interface IEjecutorTransaccionSerializable
{
    Task<T> EjecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default);
}
