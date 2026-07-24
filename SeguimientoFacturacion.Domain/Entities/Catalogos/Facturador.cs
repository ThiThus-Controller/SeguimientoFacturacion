using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa a una persona responsable de generar facturas.
/// </summary>
public sealed class Facturador : EntidadBase<int>
{
    /// <summary>
    /// Longitud máxima permitida para el nombre.
    /// </summary>
    public const int NombreLongitudMaxima = 500;

    private Facturador()
    {
    }

    /// <summary>
    /// Inicializa un nuevo facturador.
    /// </summary>
    /// <param name="id">Código único del facturador.</param>
    /// <param name="nombre">Nombre completo.</param>
    public Facturador(
        int id,
        string nombre)
        : base(ValidarId(id))
    {
        Nombre = ValidarNombre(nombre);
    }

    /// <summary>
    /// Obtiene el nombre completo del facturador.
    /// </summary>
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>
    /// Cambia el nombre del facturador.
    /// </summary>
    /// <param name="nombre">Nuevo nombre completo.</param>
    public void ActualizarNombre(string nombre)
    {
        Nombre = ValidarNombre(nombre);
    }

    private static int ValidarId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "El código del facturador debe ser mayor que cero.");
        }

        return id;
    }

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre del facturador es obligatorio.",
                nameof(nombre));
        }

        var nombreNormalizado = nombre.Trim();

        if (nombreNormalizado.Length > NombreLongitudMaxima)
        {
            throw new ArgumentException(
                $"El nombre no puede superar los {NombreLongitudMaxima} caracteres.",
                nameof(nombre));
        }

        return nombreNormalizado;
    }
}