using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Sigh.Administracion.Aplicacion.Seguridad.RegistrarAccesoDenegado;
using Sigh.Api.Configuracion;

namespace Sigh.Api.Autorizacion;

/// <summary>
/// Cada 403 de la API queda registrado como ACCESO_DENEGADO con el usuario, la ruta y el permiso que
/// faltó (EQ-04). La respuesta sigue siendo la estándar, sin información adicional.
/// </summary>
public sealed class ManejadorResultadoAutorizacion : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _predeterminado = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            var casoUso = context.RequestServices.GetRequiredService<RegistrarAccesoDenegadoCasoUso>();
            var actor = Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : (Guid?)null;
            await casoUso.EjecutarAsync(new RegistrarAccesoDenegadoComando(actor, context.Request.Method,
                context.Request.Path, Motivo(authorizeResult)), context.RequestAborted);
        }

        await _predeterminado.HandleAsync(next, context, policy, authorizeResult);
    }

    private static string Motivo(PolicyAuthorizationResult resultado)
    {
        var fallidos = resultado.AuthorizationFailure?.FailedRequirements.ToList() ?? [];

        var permisos = fallidos.OfType<ClaimsAuthorizationRequirement>()
            .Where(r => r.ClaimType == ConfiguracionJwt.ClaimPermiso)
            .SelectMany(r => r.AllowedValues ?? [])
            .ToList();
        if (permisos.Count > 0)
            return $"permiso requerido: {string.Join(", ", permisos)}";

        return fallidos.OfType<RequisitoSinCambioPendiente>().Any()
            ? "cambio de contraseña pendiente"
            : "sin autorización";
    }
}
