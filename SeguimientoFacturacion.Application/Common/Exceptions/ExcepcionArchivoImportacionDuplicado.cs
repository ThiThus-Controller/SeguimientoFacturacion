using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Common.Exceptions;

/// <summary>
/// Representa el intento de registrar nuevamente
/// un archivo con el mismo contenido.
/// </summary>
public sealed class ExcepcionArchivoImportacionDuplicado :
    Exception
{
    /// <summary>
    /// Inicializa la excepción de archivo duplicado.
    /// </summary>
    public ExcepcionArchivoImportacionDuplicado(
        TipoImportacion tipo,
        string hashArchivo)
        : base(
            "El archivo ya fue registrado previamente " +
            "para el mismo tipo de importación.")
    {
        if (!Enum.IsDefined(
                typeof(TipoImportacion),
                tipo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo de importación no es válido.");
        }

        if (string.IsNullOrWhiteSpace(hashArchivo))
        {
            throw new ArgumentException(
                "El hash del archivo es obligatorio.",
                nameof(hashArchivo));
        }

        Tipo = tipo;
        HashArchivo = hashArchivo.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Obtiene el tipo de importación solicitado.
    /// </summary>
    public TipoImportacion Tipo { get; }

    /// <summary>
    /// Obtiene la huella del archivo duplicado.
    /// </summary>
    public string HashArchivo { get; }
}