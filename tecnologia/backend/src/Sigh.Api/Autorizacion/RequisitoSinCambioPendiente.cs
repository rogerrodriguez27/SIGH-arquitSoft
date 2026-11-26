using Microsoft.AspNetCore.Authorization;
using Sigh.Api.Configuracion;
using Sigh.Compartido.Autorizacion;

namespace Sigh.Api.Autorizacion;

/// <summary>
/// Mientras el token tenga cambio_contrasena_pendiente, solo se permiten los endpoints marcados con
/// <see cref="PermitirConCambioContrasenaPendienteAttribute"/> (FR-006, research D4).
/// </summary>
public sealed class RequisitoSinCambioPendiente : IAuthorizationRequirement;

public sealed class ManejadorSinCambioPendiente : AuthorizationHandler<RequisitoSinCambioPendiente>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RequisitoSinCambioPendiente requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;                     // lo rechaza RequireAuthenticatedUser con 401

        var pendiente = context.User.HasClaim(ConfiguracionJwt.ClaimCambioContrasenaPendiente, "true");
        var endpointPermitido = context.Resource is HttpContext http
            && http.GetEndpoint()?.Metadata.GetMetadata<PermitirConCambioContrasenaPendienteAttribute>() is not null;

        if (!pendiente || endpointPermitido)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
