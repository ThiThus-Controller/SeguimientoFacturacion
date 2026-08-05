using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa una aseguradora responsable del pago de facturas.
/// </summary>
public sealed class Aseguradora : CatalogoAdministrableBase
{
    private Aseguradora()
    {
    }

    /// <summary>
    /// Inicializa una nueva aseguradora.
    /// </summary>
    public Aseguradora(
        int id,
        string descripcion)
        : this(id, descripcion, activo: true)
    {
    }

    /// <summary>
    /// Reconstruye una aseguradora indicando su estado actual.
    /// </summary>
    public Aseguradora(
        int id,
        string descripcion,
        bool activo)
        : base(id, descripcion, activo)
    {
    }
}
