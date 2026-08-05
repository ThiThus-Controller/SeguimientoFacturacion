using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.ValueObjects;
using SeguimientoFacturacion.Infrastructure.Security;

namespace SeguimientoFacturacion.Infrastructure.Security.Storage;

internal static class MapeadorUsuarioAlmacenado
{
    public static UsuarioAlmacenado Almacenar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (usuario.FechaCreacionUtc == default ||
            string.IsNullOrWhiteSpace(usuario.CreadoPor))
        {
            throw new InvalidOperationException(
                "El usuario debe tener auditoría de creación antes de guardarse.");
        }

        return new UsuarioAlmacenado
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Roles = usuario.Roles.Order().ToList(),
            Activo = usuario.Activo,
            VersionSeguridad = usuario.VersionSeguridad,
            PermisosConcedidos = usuario.PermisosConcedidos
                .Order(StringComparer.Ordinal)
                .ToList(),
            PermisosRevocados = usuario.PermisosRevocados
                .Order(StringComparer.Ordinal)
                .ToList(),
            Credencial = new CredencialAlmacenada
            {
                Algoritmo = ProcesadorCredencialesPbkdf2.Algoritmo,
                HashContrasena = usuario.Credencial.HashContrasena,
                SaltContrasena = usuario.Credencial.SaltContrasena,
                IteracionesPbkdf2 =
                    usuario.Credencial.IteracionesPbkdf2,
                Version = usuario.Credencial.Version
            },
            FechaCreacionUtc = usuario.FechaCreacionUtc,
            CreadoPor = usuario.CreadoPor,
            FechaModificacionUtc = usuario.FechaModificacionUtc,
            ModificadoPor = usuario.ModificadoPor
        };
    }

    public static Usuario Restaurar(UsuarioAlmacenado datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        if (datos.FechaCreacionUtc == default ||
            string.IsNullOrWhiteSpace(datos.CreadoPor))
        {
            throw new InvalidDataException(
                "El usuario almacenado no contiene auditoría de creación.");
        }

        if (datos.FechaModificacionUtc.HasValue !=
            !string.IsNullOrWhiteSpace(datos.ModificadoPor))
        {
            throw new InvalidDataException(
                "La auditoría de modificación del usuario está incompleta.");
        }

        if (datos.Credencial is null)
        {
            throw new InvalidDataException(
                "El usuario almacenado no contiene una credencial.");
        }

        if (!string.Equals(
                datos.Credencial.Algoritmo,
                ProcesadorCredencialesPbkdf2.Algoritmo,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "El algoritmo de la credencial almacenada no es compatible.");
        }

        var credencial = new CredencialUsuario(
            datos.Credencial.HashContrasena,
            datos.Credencial.SaltContrasena,
            datos.Credencial.IteracionesPbkdf2,
            datos.Credencial.Version);

        var usuario = new Usuario(
            datos.Id,
            datos.NombreUsuario,
            datos.NombreCompleto,
            datos.Roles,
            credencial,
            datos.Activo,
            datos.PermisosConcedidos,
            datos.PermisosRevocados,
            datos.VersionSeguridad);

        usuario.RegistrarCreacion(
            datos.FechaCreacionUtc,
            datos.CreadoPor);

        if (datos.FechaModificacionUtc.HasValue)
        {
            usuario.RegistrarModificacion(
                datos.FechaModificacionUtc.Value,
                datos.ModificadoPor!);
        }

        return usuario;
    }
}
