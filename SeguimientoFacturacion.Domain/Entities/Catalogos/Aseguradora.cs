using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa una aseguradora responsable del pago de facturas.
/// </summary>
public sealed class Aseguradora : CatalogoBase
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
        : base(id, descripcion)
    {
    }
}