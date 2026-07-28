using System.Security.Cryptography;
using SeguimientoFacturacion.Application.Interfaces.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Calcula huellas SHA-256 para identificar
/// archivos de importación.
/// </summary>
public sealed class CalculadorHashArchivoSha256 :
    ICalculadorHashArchivo
{
    /// <inheritdoc />
    public async Task<string> CalcularSha256Async(
        Stream contenido,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        if (!contenido.CanRead)
        {
            throw new ArgumentException(
                "El flujo del archivo debe permitir lectura.",
                nameof(contenido));
        }

        if (!contenido.CanSeek)
        {
            throw new NotSupportedException(
                "El flujo del archivo debe permitir cambiar " +
                "su posición para calcular el hash sin " +
                "afectar el análisis posterior.");
        }

        var posicionOriginal = contenido.Position;

        try
        {
            contenido.Position = 0;

            using var sha256 = SHA256.Create();

            var hash = await sha256.ComputeHashAsync(
                contenido,
                cancellationToken);

            return Convert.ToHexString(hash);
        }
        finally
        {
            contenido.Position = posicionOriginal;
        }
    }
}