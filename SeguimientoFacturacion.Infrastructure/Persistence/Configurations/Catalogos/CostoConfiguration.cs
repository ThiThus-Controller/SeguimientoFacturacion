using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de los centros o categorías de costo.
/// </summary>
internal sealed class CostoConfiguration :
    CatalogoConfigurationBase<Costo>
{
    protected override string NombreTabla => "Costos";
}