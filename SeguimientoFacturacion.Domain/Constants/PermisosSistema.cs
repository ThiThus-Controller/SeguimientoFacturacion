using System.Collections.Frozen;

namespace SeguimientoFacturacion.Domain.Constants;

/// <summary>
/// Define el catálogo cerrado de permisos reconocidos por el sistema.
/// Los códigos son estables porque se almacenarán en usuarios.dat y
/// se publicarán como claims de autorización.
/// </summary>
public static class PermisosSistema
{
    /// <summary>
    /// Permisos relacionados con facturas.
    /// </summary>
    public static class Facturas
    {
        public const string Ver = "Facturas.Ver";
        public const string Crear = "Facturas.Crear";
        public const string Importar = "Facturas.Importar";
        public const string Confirmar = "Facturas.Confirmar";
        public const string Procesar = "Facturas.Procesar";
        public const string Editar = "Facturas.Editar";
        public const string Anular = "Facturas.Anular";
    }

    /// <summary>
    /// Permisos relacionados con pacientes.
    /// </summary>
    public static class Pacientes
    {
        public const string Ver = "Pacientes.Ver";
        public const string Crear = "Pacientes.Crear";
        public const string Importar = "Pacientes.Importar";
        public const string Editar = "Pacientes.Editar";
        public const string Inactivar = "Pacientes.Inactivar";
    }

    /// <summary>
    /// Permisos relacionados con notas crédito.
    /// </summary>
    public static class NotasCredito
    {
        public const string Ver = "NotasCredito.Ver";
        public const string Crear = "NotasCredito.Crear";
        public const string Importar = "NotasCredito.Importar";
        public const string Confirmar = "NotasCredito.Confirmar";
        public const string Procesar = "NotasCredito.Procesar";
        public const string Editar = "NotasCredito.Editar";
        public const string Anular = "NotasCredito.Anular";
    }

    /// <summary>
    /// Permisos relacionados con notas débito.
    /// </summary>
    public static class NotasDebito
    {
        public const string Ver = "NotasDebito.Ver";
        public const string Crear = "NotasDebito.Crear";
        public const string Importar = "NotasDebito.Importar";
        public const string Confirmar = "NotasDebito.Confirmar";
        public const string Procesar = "NotasDebito.Procesar";
        public const string Editar = "NotasDebito.Editar";
        public const string Anular = "NotasDebito.Anular";
    }

    /// <summary>
    /// Permisos relacionados con glosas, respuestas y conciliaciones.
    /// </summary>
    public static class Glosas
    {
        public const string Ver = "Glosas.Ver";
        public const string Crear = "Glosas.Crear";
        public const string Importar = "Glosas.Importar";
        public const string Confirmar = "Glosas.Confirmar";
        public const string Procesar = "Glosas.Procesar";
        public const string Editar = "Glosas.Editar";
        public const string Responder = "Glosas.Responder";
        public const string Conciliar = "Glosas.Conciliar";
        public const string Anular = "Glosas.Anular";
    }

    /// <summary>
    /// Permisos relacionados con pagos.
    /// </summary>
    public static class Pagos
    {
        public const string Ver = "Pagos.Ver";
        public const string Crear = "Pagos.Crear";
        public const string Importar = "Pagos.Importar";
        public const string Confirmar = "Pagos.Confirmar";
        public const string Procesar = "Pagos.Procesar";
        public const string Editar = "Pagos.Editar";
        public const string Anular = "Pagos.Anular";
    }

    /// <summary>
    /// Permisos relacionados con aplicaciones de pago.
    /// </summary>
    public static class AplicacionesPago
    {
        public const string Ver = "AplicacionesPago.Ver";
        public const string Crear = "AplicacionesPago.Crear";
        public const string Editar = "AplicacionesPago.Editar";
        public const string Reversar = "AplicacionesPago.Reversar";
    }

    /// <summary>
    /// Permisos relacionados con usuarios y autorización.
    /// </summary>
    public static class Usuarios
    {
        public const string Ver = "Usuarios.Ver";
        public const string Crear = "Usuarios.Crear";
        public const string Editar = "Usuarios.Editar";
        public const string Inactivar = "Usuarios.Inactivar";
        public const string RestablecerClave = "Usuarios.RestablecerClave";
        public const string AsignarRoles = "Usuarios.AsignarRoles";
        public const string AsignarPermisos = "Usuarios.AsignarPermisos";
    }

    /// <summary>
    /// Permisos relacionados con perfiles de rol.
    /// </summary>
    public static class Roles
    {
        public const string Ver = "Roles.Ver";
        public const string Crear = "Roles.Crear";
        public const string Editar = "Roles.Editar";
        public const string AsignarPermisos = "Roles.AsignarPermisos";
    }

    /// <summary>
    /// Permisos administrativos del catálogo de aseguradoras.
    /// </summary>
    public static class Aseguradoras
    {
        public const string Ver = "Aseguradoras.Ver";
        public const string Crear = "Aseguradoras.Crear";
        public const string Editar = "Aseguradoras.Editar";
        public const string Inactivar = "Aseguradoras.Inactivar";
    }

    /// <summary>
    /// Permisos administrativos del catálogo de facturadores.
    /// </summary>
    public static class Facturadores
    {
        public const string Ver = "Facturadores.Ver";
        public const string Crear = "Facturadores.Crear";
        public const string Editar = "Facturadores.Editar";
        public const string Inactivar = "Facturadores.Inactivar";
    }

    /// <summary>
    /// Permisos del tablero administrativo.
    /// </summary>
    public static class Dashboard
    {
        public const string Ver = "Dashboard.Ver";
    }

    private static readonly FrozenSet<string> PermisosRegistrados =
        new[]
        {
            Facturas.Ver,
            Facturas.Crear,
            Facturas.Importar,
            Facturas.Confirmar,
            Facturas.Procesar,
            Facturas.Editar,
            Facturas.Anular,
            Pacientes.Ver,
            Pacientes.Crear,
            Pacientes.Importar,
            Pacientes.Editar,
            Pacientes.Inactivar,
            NotasCredito.Ver,
            NotasCredito.Crear,
            NotasCredito.Importar,
            NotasCredito.Confirmar,
            NotasCredito.Procesar,
            NotasCredito.Editar,
            NotasCredito.Anular,
            NotasDebito.Ver,
            NotasDebito.Crear,
            NotasDebito.Importar,
            NotasDebito.Confirmar,
            NotasDebito.Procesar,
            NotasDebito.Editar,
            NotasDebito.Anular,
            Glosas.Ver,
            Glosas.Crear,
            Glosas.Importar,
            Glosas.Confirmar,
            Glosas.Procesar,
            Glosas.Editar,
            Glosas.Responder,
            Glosas.Conciliar,
            Glosas.Anular,
            Pagos.Ver,
            Pagos.Crear,
            Pagos.Importar,
            Pagos.Confirmar,
            Pagos.Procesar,
            Pagos.Editar,
            Pagos.Anular,
            AplicacionesPago.Ver,
            AplicacionesPago.Crear,
            AplicacionesPago.Editar,
            AplicacionesPago.Reversar,
            Usuarios.Ver,
            Usuarios.Crear,
            Usuarios.Editar,
            Usuarios.Inactivar,
            Usuarios.RestablecerClave,
            Usuarios.AsignarRoles,
            Usuarios.AsignarPermisos,
            Roles.Ver,
            Roles.Crear,
            Roles.Editar,
            Roles.AsignarPermisos,
            Aseguradoras.Ver,
            Aseguradoras.Crear,
            Aseguradoras.Editar,
            Aseguradoras.Inactivar,
            Facturadores.Ver,
            Facturadores.Crear,
            Facturadores.Editar,
            Facturadores.Inactivar,
            Dashboard.Ver
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene todos los permisos admitidos por la aplicación.
    /// </summary>
    public static IReadOnlySet<string> Todos => PermisosRegistrados;

    /// <summary>
    /// Determina si el código pertenece al catálogo de permisos.
    /// </summary>
    public static bool EsValido(string? permiso)
    {
        return !string.IsNullOrWhiteSpace(permiso) &&
            PermisosRegistrados.Contains(permiso.Trim());
    }

    /// <summary>
    /// Valida un permiso y devuelve su código canónico.
    /// </summary>
    public static string Normalizar(string permiso)
    {
        if (string.IsNullOrWhiteSpace(permiso))
        {
            throw new ArgumentException(
                "El permiso es obligatorio.",
                nameof(permiso));
        }

        var valor = permiso.Trim();
        var permisoCanonico = PermisosRegistrados.FirstOrDefault(
            permisoRegistrado => string.Equals(
                permisoRegistrado,
                valor,
                StringComparison.OrdinalIgnoreCase));

        return permisoCanonico ?? throw new ArgumentException(
            $"El permiso '{valor}' no está registrado en el sistema.",
            nameof(permiso));
    }
}
