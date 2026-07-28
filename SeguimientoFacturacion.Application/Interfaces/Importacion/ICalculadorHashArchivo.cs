namespace SeguimientoFacturacion.Application.Interfaces.Importacion;

/// <summary>
/// Define el cálculo de una huella criptográfica
/// para identificar el contenido de un archivo.
/// </summary>
public interface ICalculadorHashArchivo
{
    /// <summary>
    /// Calcula el hash SHA-256 del contenido completo.
    /// </summary>
    /// <param name="contenido">
    /// Flujo legible y posicionable cuyo contenido
    /// será procesado desde el inicio.
    /// </param>
    /// <param name="cancellationToken">
    /// Token utilizado para cancelar la operación.
    /// </param>
    /// <returns>
    /// Hash hexadecimal SHA-256 de 64 caracteres
    /// en mayúsculas.
    /// </returns>
    /// <remarks>
    /// La implementación debe restaurar la posición
    /// original del flujo antes de finalizar.
    /// </remarks>
    Task<string> CalcularSha256Async(
        Stream contenido,
        CancellationToken cancellationToken = default);
}