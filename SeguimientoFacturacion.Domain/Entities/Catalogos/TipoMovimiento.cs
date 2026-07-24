using SeguimientoFacturacion.Domain.Common;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa uno de los tipos de movimiento definidos
/// en la tabla T_MOV.
/// </summary>
public sealed class TipoMovimiento :
    EntidadBase<TipoMovimientoCodigo>
{
    /// <summary>
    /// Longitud máxima permitida para la descripción.
    /// </summary>
    public const int DescripcionLongitudMaxima = 100;

    private TipoMovimiento()
    {
    }

    /// <summary>
    /// Inicializa un nuevo tipo de movimiento.
    /// </summary>
    /// <param name="codigo">Código oficial del movimiento.</param>
    /// <param name="descripcion">Descripción del movimiento.</param>
    public TipoMovimiento(
        TipoMovimientoCodigo codigo,
        string descripcion)
        : base(ValidarCodigo(codigo))
    {
        Descripcion = ValidarDescripcion(descripcion);
    }

    /// <summary>
    /// Obtiene la descripción del movimiento.
    /// </summary>
    public string Descripcion { get; private set; } = string.Empty;

    /// <summary>
    /// Cambia la descripción del movimiento.
    /// </summary>
    /// <param name="descripcion">Nueva descripción.</param>
    public void ActualizarDescripcion(string descripcion)
    {
        Descripcion = ValidarDescripcion(descripcion);
    }

    private static TipoMovimientoCodigo ValidarCodigo(
        TipoMovimientoCodigo codigo)
    {
        if (!Enum.IsDefined(typeof(TipoMovimientoCodigo), codigo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(codigo),
                codigo,
                "El código del tipo de movimiento no es válido.");
        }

        return codigo;
    }

    private static string ValidarDescripcion(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException(
                "La descripción del tipo de movimiento es obligatoria.",
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