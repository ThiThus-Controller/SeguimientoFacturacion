namespace SeguimientoFacturacion.Domain.Common;

/// <summary>
/// Representa un catálogo con estado lógico y auditoría administrativa.
/// </summary>
public abstract class CatalogoAdministrableBase :
    EntidadAuditableBase<int>
{
    /// <summary>
    /// Longitud máxima de la descripción del catálogo.
    /// </summary>
    public const int DescripcionLongitudMaxima = 100;

    protected CatalogoAdministrableBase()
    {
    }

    protected CatalogoAdministrableBase(
        int id,
        string descripcion,
        bool activo = true)
        : base(ValidarId(id))
    {
        Descripcion = ValidarDescripcion(descripcion);
        Activo = activo;
    }

    /// <summary>
    /// Obtiene la descripción visible del registro.
    /// </summary>
    public string Descripcion { get; private set; } = string.Empty;

    /// <summary>
    /// Indica si el registro puede utilizarse en nuevas operaciones.
    /// </summary>
    public bool Activo { get; private set; }

    /// <summary>
    /// Actualiza y normaliza la descripción.
    /// </summary>
    public void ActualizarDescripcion(string descripcion)
    {
        Descripcion = ValidarDescripcion(descripcion);
    }

    /// <summary>
    /// Habilita el registro para nuevas operaciones.
    /// </summary>
    public void Activar()
    {
        Activo = true;
    }

    /// <summary>
    /// Inhabilita el registro sin eliminar su información histórica.
    /// </summary>
    public void Desactivar()
    {
        Activo = false;
    }

    private static int ValidarId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "El código del catálogo debe ser mayor que cero.");
        }

        return id;
    }

    private static string ValidarDescripcion(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException(
                "La descripción es obligatoria.",
                nameof(descripcion));
        }

        var descripcionNormalizada = descripcion.Trim();

        if (descripcionNormalizada.Length > DescripcionLongitudMaxima)
        {
            throw new ArgumentException(
                $"La descripción no puede superar los " +
                $"{DescripcionLongitudMaxima} caracteres.",
                nameof(descripcion));
        }

        return descripcionNormalizada;
    }
}
