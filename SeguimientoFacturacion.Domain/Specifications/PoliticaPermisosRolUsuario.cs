using System.Collections.Frozen;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Specifications;

/// <summary>
/// Define los permisos heredados por los perfiles de rol del sistema.
/// Las concesiones y revocaciones particulares del usuario se aplican
/// posteriormente sobre este resultado.
/// </summary>
public static class PoliticaPermisosRolUsuario
{
    private static readonly FrozenSet<string> SinPermisos =
        Array.Empty<string>().ToFrozenSet(
            StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> PermisosConsulta =
        new[]
        {
            PermisosSistema.Facturas.Ver,
            PermisosSistema.Pacientes.Ver,
            PermisosSistema.NotasCredito.Ver,
            PermisosSistema.NotasDebito.Ver,
            PermisosSistema.Glosas.Ver,
            PermisosSistema.Pagos.Ver,
            PermisosSistema.AplicacionesPago.Ver,
            PermisosSistema.Aseguradoras.Ver,
            PermisosSistema.Facturadores.Ver,
            PermisosSistema.Dashboard.Ver
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> PermisosSupervisor =
        PermisosConsulta
            .Concat(
                new[]
                {
                    PermisosSistema.Facturas.Confirmar,
                    PermisosSistema.Facturas.Procesar,
                    PermisosSistema.Facturas.Editar,
                    PermisosSistema.Pacientes.Editar,
                    PermisosSistema.NotasCredito.Confirmar,
                    PermisosSistema.NotasCredito.Procesar,
                    PermisosSistema.NotasCredito.Editar,
                    PermisosSistema.NotasDebito.Confirmar,
                    PermisosSistema.NotasDebito.Procesar,
                    PermisosSistema.NotasDebito.Editar,
                    PermisosSistema.Glosas.Confirmar,
                    PermisosSistema.Glosas.Procesar,
                    PermisosSistema.Glosas.Editar,
                    PermisosSistema.Glosas.Responder,
                    PermisosSistema.Glosas.Conciliar,
                    PermisosSistema.Pagos.Confirmar,
                    PermisosSistema.Pagos.Procesar,
                    PermisosSistema.Pagos.Editar,
                    PermisosSistema.AplicacionesPago.Crear,
                    PermisosSistema.AplicacionesPago.Editar
                })
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> PermisosOperadorFacturas =
        new[]
        {
            PermisosSistema.Facturas.Ver,
            PermisosSistema.Facturas.Crear,
            PermisosSistema.Facturas.Importar,
            PermisosSistema.Facturas.Editar,
            PermisosSistema.Pacientes.Ver,
            PermisosSistema.Pacientes.Crear,
            PermisosSistema.Pacientes.Importar,
            PermisosSistema.Pacientes.Editar,
            PermisosSistema.Aseguradoras.Ver,
            PermisosSistema.Facturadores.Ver
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> PermisosOperadorNotas =
        new[]
        {
            PermisosSistema.Facturas.Ver,
            PermisosSistema.NotasCredito.Ver,
            PermisosSistema.NotasCredito.Crear,
            PermisosSistema.NotasCredito.Importar,
            PermisosSistema.NotasCredito.Editar,
            PermisosSistema.NotasDebito.Ver,
            PermisosSistema.NotasDebito.Crear,
            PermisosSistema.NotasDebito.Importar,
            PermisosSistema.NotasDebito.Editar,
            PermisosSistema.Aseguradoras.Ver
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> PermisosOperadorGlosas =
        new[]
        {
            PermisosSistema.Facturas.Ver,
            PermisosSistema.Glosas.Ver,
            PermisosSistema.Glosas.Crear,
            PermisosSistema.Glosas.Importar,
            PermisosSistema.Glosas.Editar,
            PermisosSistema.Glosas.Responder,
            PermisosSistema.Glosas.Conciliar,
            PermisosSistema.Aseguradoras.Ver
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> PermisosOperadorCartera =
        new[]
        {
            PermisosSistema.Facturas.Ver,
            PermisosSistema.Pagos.Ver,
            PermisosSistema.Pagos.Crear,
            PermisosSistema.Pagos.Importar,
            PermisosSistema.Pagos.Editar,
            PermisosSistema.AplicacionesPago.Ver,
            PermisosSistema.AplicacionesPago.Crear,
            PermisosSistema.AplicacionesPago.Editar,
            PermisosSistema.Aseguradoras.Ver
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Obtiene los permisos predeterminados de un rol.
    /// </summary>
    public static IReadOnlySet<string> ObtenerPermisos(
        RolUsuario rol)
    {
        ValidarRol(rol);

        return rol switch
        {
            RolUsuario.Administrador => PermisosSistema.Todos,
            RolUsuario.Supervisor => PermisosSupervisor,
            RolUsuario.OperadorFacturas => PermisosOperadorFacturas,
            RolUsuario.OperadorNotas => PermisosOperadorNotas,
            RolUsuario.OperadorGlosas => PermisosOperadorGlosas,
            RolUsuario.OperadorCartera => PermisosOperadorCartera,
            RolUsuario.Consulta => PermisosConsulta,
            RolUsuario.Personalizado => SinPermisos,
            _ => throw new ArgumentOutOfRangeException(nameof(rol))
        };
    }

    /// <summary>
    /// Combina sin duplicados los permisos heredados de varios roles.
    /// </summary>
    public static IReadOnlySet<string> ObtenerPermisos(
        IEnumerable<RolUsuario> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var resultado = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var rol in roles.Distinct())
        {
            resultado.UnionWith(ObtenerPermisos(rol));
        }

        return resultado.ToFrozenSet(
            StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidarRol(RolUsuario rol)
    {
        if (!Enum.IsDefined(rol))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rol),
                rol,
                "El rol indicado no es válido.");
        }
    }
}
