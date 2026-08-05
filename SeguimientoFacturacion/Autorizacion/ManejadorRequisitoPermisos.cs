using Microsoft.AspNetCore.Authorization;
using SeguimientoFacturacion.Configurations;

namespace SeguimientoFacturacion.Autorizacion;

/// <summary>
/// Evalúa los permisos efectivos publicados en la identidad autenticada.
/// </summary>
public sealed class ManejadorRequisitoPermisos :
    AuthorizationHandler<RequisitoPermisos>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequisitoPermisos requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var permisos = context.User
            .FindAll(NombresSeguridadWeb.ClaimPermiso)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var autorizado = requirement.Alternativas.Any(
            alternativa => alternativa.All(permisos.Contains));

        if (autorizado)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
