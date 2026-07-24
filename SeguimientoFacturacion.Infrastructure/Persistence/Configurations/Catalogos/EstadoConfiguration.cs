using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de los estados de facturación.
/// </summary>
internal sealed class EstadoConfiguration :
    CatalogoConfigurationBase<Estado>
{
    protected override string NombreTabla => "Estados";
}