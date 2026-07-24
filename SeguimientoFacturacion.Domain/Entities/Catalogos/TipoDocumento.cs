using SeguimientoFacturacion.Domain.Common;

namespace SeguimientoFacturacion.Domain.Entities.Catalogos;

/// <summary>
/// Representa un tipo de documento de identificación.
/// </summary>
public sealed class TipoDocumento : CatalogoBase
{
    /// <summary>
    /// Longitud máxima permitida para la sigla.
    /// </summary>
    public const int SiglaLongitudMaxima = 20;

    private TipoDocumento()
    {
    }

    /// <summary>
    /// Inicializa un nuevo tipo de documento.
    /// </summary>
    /// <param name="id">Código único.</param>
    /// <param name="descripcion">Descripción del documento.</param>
    /// <param name="sigla">Sigla utilizada para identificarlo.</param>
    public TipoDocumento(
        int id,
        string descripcion,
        string sigla)
        : base(id, descripcion)
    {
        Sigla = ValidarSigla(sigla);
    }

    /// <summary>
    /// Obtiene la sigla del tipo de documento.
    /// </summary>
    public string Sigla { get; private set; } = string.Empty;

    /// <summary>
    /// Cambia la sigla del tipo de documento.
    /// </summary>
    /// <param name="sigla">Nueva sigla.</param>
    public void ActualizarSigla(string sigla)
    {
        Sigla = ValidarSigla(sigla);
    }

    private static string ValidarSigla(string sigla)
    {
        if (string.IsNullOrWhiteSpace(sigla))
        {
            throw new ArgumentException(
                "La sigla del tipo de documento es obligatoria.",
                nameof(sigla));
        }

        var siglaNormalizada = sigla.Trim().ToUpperInvariant();

        if (siglaNormalizada.Length > SiglaLongitudMaxima)
        {
            throw new ArgumentException(
                $"La sigla no puede superar los {SiglaLongitudMaxima} caracteres.",
                nameof(sigla));
        }

        return siglaNormalizada;
    }
}