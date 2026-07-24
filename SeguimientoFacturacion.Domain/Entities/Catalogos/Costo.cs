using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa un centro o categoría de costo.
/// </summary>
public sealed class Costo : CatalogoBase
{
    private Costo()
    {
    }

    /// <summary>
    /// Inicializa un nuevo centro o categoría de costo.
    /// </summary>
    public Costo(
        int id,
        string descripcion)
        : base(id, descripcion)
    {
    }
}