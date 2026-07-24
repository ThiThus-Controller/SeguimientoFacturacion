using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de las aseguradoras.
/// </summary>
internal sealed class AseguradoraConfiguration :
    CatalogoConfigurationBase<Aseguradora>
{
    protected override string NombreTabla => "Aseguradoras";
}