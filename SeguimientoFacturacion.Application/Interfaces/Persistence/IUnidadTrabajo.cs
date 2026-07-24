namespace SeguimientoFacturacion.Application.Interfaces.Persistence;

/// <summary>
/// Coordina la persistencia atómica de los cambios.
/// </summary>
public interface IUnidadTrabajo
{
    /// <summary>
    /// Guarda los cambios pendientes.
    /// </summary>
    /// <returns>
    /// Cantidad de registros afectados.
    /// </returns>
    Task<int> GuardarCambiosAsync(
        CancellationToken cancellationToken = default);
}