using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa un estado asociado al seguimiento de una factura.
/// </summary>
public sealed class Estado : CatalogoBase
{
    private Estado()
    {
    }

    /// <summary>
    /// Inicializa un nuevo estado.
    /// </summary>
    public Estado(
        int id,
        string descripcion)
        : base(id, descripcion)
    {
    }
}