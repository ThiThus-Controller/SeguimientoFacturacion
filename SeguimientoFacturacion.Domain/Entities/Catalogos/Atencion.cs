using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa un tipo de atención prestada al paciente.
/// </summary>
public sealed class Atencion : CatalogoBase
{
    private Atencion()
    {
    }

    /// <summary>
    /// Inicializa un nuevo tipo de atención.
    /// </summary>
    public Atencion(
        int id,
        string descripcion)
        : base(id, descripcion)
    {
    }
}