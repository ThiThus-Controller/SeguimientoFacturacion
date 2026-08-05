namespace SeguimientoFacturacion.Infrastructure.Encryption;

/// <summary>
/// Representa un fallo al cifrar o validar el contenido de usuarios.dat.
/// No expone datos criptográficos sensibles en el mensaje.
/// </summary>
public sealed class ExcepcionProteccionUsuarios : Exception
{
    public ExcepcionProteccionUsuarios(string mensaje)
        : base(mensaje)
    {
    }

    public ExcepcionProteccionUsuarios(
        string mensaje,
        Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
