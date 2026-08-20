using System.Data;
using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Interfaces.Persistence;

namespace SeguimientoFacturacion.Infrastructure.Persistence;

/// <summary>
/// Protege operaciones financieras consolidadas contra consumos concurrentes.
/// </summary>
public sealed class EjecutorTransaccionSerializableEfCore :
    IEjecutorTransaccionSerializable
{
    private readonly SeguimientoDbContext _contexto;

    public EjecutorTransaccionSerializableEfCore(
        SeguimientoDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        _contexto = contexto;
    }

    public Task<T> EjecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        var estrategia = _contexto.Database.CreateExecutionStrategy();

        return estrategia.ExecuteAsync(
            async () =>
            {
                await using var transaccion = await _contexto.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                var resultado = await operacion(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);
                return resultado;
            });
    }
}
