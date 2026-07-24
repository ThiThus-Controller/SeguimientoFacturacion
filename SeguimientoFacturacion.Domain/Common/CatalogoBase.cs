namespace SeguimientoFacturacion.Domain.Common;

/// <summary>
/// Representa la clase base para los catálogos identificados
/// mediante un código numérico.
/// </summary>
public abstract class CatalogoBase : EntidadBase<int>
{
    /// <summary>
    /// Longitud máxima permitida para la descripción.
    /// </summary>
    public const int DescripcionLongitudMaxima = 100;

    /// <summary>
    /// Inicializa una instancia vacía para permitir
    /// la reconstrucción de la entidad.
    /// </summary>
    protected CatalogoBase()
    {
    }

    /// <summary>
    /// Inicializa una nueva entidad de catálogo.
    /// </summary>
    /// <param name="id">Código único del catálogo.</param>
    /// <param name="descripcion">Descripción del registro.</param>
    protected CatalogoBase(
        int id,
        string descripcion)
        : base(ValidarId(id))
    {
        Descripcion = ValidarDescripcion(descripcion);
    }

    /// <summary>
    /// Obtiene la descripción del registro.
    /// </summary>
    public string Descripcion { get; private set; } = string.Empty;

    /// <summary>
    /// Cambia la descripción del registro.
    /// </summary>
    /// <param name="descripcion">Nueva descripción.</param>
    public void ActualizarDescripcion(string descripcion)
    {
        Descripcion = ValidarDescripcion(descripcion);
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
                $"La descripción no puede superar los {DescripcionLongitudMaxima} caracteres.",
                nameof(descripcion));
        }

        return descripcionNormalizada;
    }
}