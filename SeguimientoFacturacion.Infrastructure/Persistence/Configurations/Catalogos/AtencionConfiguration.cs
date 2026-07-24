using SeguimientoFacturacion.Domain.Entities.Catalogos;

namespace SeguimientoFacturacion.Infrastructure.Persistence.Configurations.Catalogos;

/// <summary>
/// Configura la persistencia de los tipos de atención.
/// </summary>
internal sealed class AtencionConfiguration :
    CatalogoConfigurationBase<Atencion>
{
    protected override string NombreTabla => "Atenciones";
}