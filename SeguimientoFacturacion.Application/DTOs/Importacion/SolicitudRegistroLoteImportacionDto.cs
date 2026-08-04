using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa la solicitud para registrar un archivo
/// como lote de importación pendiente.
/// </summary>
public sealed record SolicitudRegistroLoteImportacionDto
{
    /// <summary>
    /// Obtiene el tipo de información que será importada.
    /// </summary>
    public TipoImportacion Tipo { get; init; }

    /// <summary>
    /// Obtiene el nombre original del archivo.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene el contenido del archivo.
    /// La capa que abre el flujo es responsable de cerrarlo.
    /// </summary>
    public required Stream Contenido { get; init; }

    /// <summary>
    /// Obtiene el usuario responsable de presentar
    /// el archivo.
    /// </summary>
    public required string Usuario { get; init; }
}