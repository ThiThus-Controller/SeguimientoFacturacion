using Microsoft.AspNetCore.Authorization;
using SeguimientoFacturacion.Autorizacion;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Configurations;

/// <summary>
/// Centraliza las políticas de autorización de la aplicación web.
/// </summary>
public static class PoliticasAutorizacion
{
    private const string PrefijoPermiso = "Permiso:";

    public const string ImportacionesAcceder =
        "Importaciones.Acceder";

    public const string AnalizarFacturas =
        "Importaciones.Analizar.Facturas";

    public const string AnalizarNotasFactura =
        "Importaciones.Analizar.NotasFactura";

    public const string AnalizarGlosas =
        "Importaciones.Analizar.Glosas";

    public const string AnalizarPagos =
        "Importaciones.Analizar.Pagos";

    public const string ConfirmarFacturas =
        PrefijoPermiso + PermisosSistema.Facturas.Confirmar;

    public const string ProcesarFacturas =
        PrefijoPermiso + PermisosSistema.Facturas.Procesar;

    public const string UsuariosConsultar =
        PrefijoPermiso + PermisosSistema.Usuarios.Ver;

    public const string UsuariosCrear =
        "Usuarios.CrearConAcceso";

    public const string UsuariosEditar =
        "Usuarios.EditarAcceso";

    public const string UsuariosCambiarEstado =
        PrefijoPermiso + PermisosSistema.Usuarios.Inactivar;

    public const string UsuariosRestablecerContrasena =
        PrefijoPermiso + PermisosSistema.Usuarios.RestablecerClave;

    private static readonly string[] PermisosCreacionUsuarios =
    [
        PermisosSistema.Usuarios.Crear,
        PermisosSistema.Usuarios.AsignarRoles,
        PermisosSistema.Usuarios.AsignarPermisos
    ];

    private static readonly string[] PermisosEdicionUsuarios =
    [
        PermisosSistema.Usuarios.Editar,
        PermisosSistema.Usuarios.AsignarRoles,
        PermisosSistema.Usuarios.AsignarPermisos
    ];

    private static readonly string[] PermisosFacturas =
    [
        PermisosSistema.Facturas.Importar,
        PermisosSistema.Pacientes.Importar
    ];

    private static readonly string[] PermisosNotasFactura =
    [
        PermisosSistema.NotasCredito.Importar,
        PermisosSistema.NotasDebito.Importar
    ];

    private static readonly string[] PermisosGlosas =
    [
        PermisosSistema.Glosas.Importar,
        PermisosSistema.Glosas.Responder
    ];

    private static readonly string[] PermisosPagos =
    [
        PermisosSistema.Pagos.Importar,
        PermisosSistema.AplicacionesPago.Crear
    ];

    /// <summary>
    /// Obtiene el nombre estable de la política de un permiso individual.
    /// </summary>
    public static string ParaPermiso(string permiso)
    {
        return PrefijoPermiso + PermisosSistema.Normalizar(permiso);
    }

    /// <summary>
    /// Obtiene la política de análisis para el tipo de plantilla indicado.
    /// </summary>
    public static string ParaAnalisis(TipoImportacion tipo)
    {
        return tipo switch
        {
            TipoImportacion.Facturas => AnalizarFacturas,
            TipoImportacion.NotasFactura => AnalizarNotasFactura,
            TipoImportacion.Glosas => AnalizarGlosas,
            TipoImportacion.Pagos => AnalizarPagos,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "El tipo no tiene una política de importación modular.")
        };
    }

    /// <summary>
    /// Registra las políticas individuales y las políticas compuestas
    /// utilizadas por las plantillas modulares.
    /// </summary>
    public static void Registrar(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (var permiso in PermisosSistema.Todos)
        {
            AgregarPolitica(
                options,
                ParaPermiso(permiso),
                RequisitoPermisos.ExigirTodos(permiso));
        }

        AgregarPolitica(
            options,
            AnalizarFacturas,
            RequisitoPermisos.ExigirTodos(PermisosFacturas));

        AgregarPolitica(
            options,
            AnalizarNotasFactura,
            RequisitoPermisos.ExigirTodos(PermisosNotasFactura));

        AgregarPolitica(
            options,
            AnalizarGlosas,
            RequisitoPermisos.ExigirTodos(PermisosGlosas));

        AgregarPolitica(
            options,
            AnalizarPagos,
            RequisitoPermisos.ExigirTodos(PermisosPagos));

        AgregarPolitica(
            options,
            ImportacionesAcceder,
            new RequisitoPermisos(
                new[]
                {
                    PermisosFacturas,
                    PermisosNotasFactura,
                    PermisosGlosas,
                    PermisosPagos
                }));

        AgregarPolitica(
            options,
            UsuariosCrear,
            RequisitoPermisos.ExigirTodos(
                PermisosCreacionUsuarios));

        AgregarPolitica(
            options,
            UsuariosEditar,
            RequisitoPermisos.ExigirTodos(
                PermisosEdicionUsuarios));
    }

    private static void AgregarPolitica(
        AuthorizationOptions options,
        string nombre,
        RequisitoPermisos requisito)
    {
        options.AddPolicy(
            nombre,
            policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(requisito);
            });
    }
}
