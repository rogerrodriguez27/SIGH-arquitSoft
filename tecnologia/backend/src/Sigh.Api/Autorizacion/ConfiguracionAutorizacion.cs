using Microsoft.AspNetCore.Authorization;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Api.Configuracion;

namespace Sigh.Api.Autorizacion;

/// <summary>
/// Una política por cada permiso fijo (research D4, FR-012), más una política de respaldo que exige
/// sesión en todos los endpoints salvo los marcados como anónimos (FR-007).
/// </summary>
public static class ConfiguracionAutorizacion
{
    public static IServiceCollection AgregarAutorizacionPorPermisos(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ManejadorSinCambioPendiente>();

        services.AddAuthorization(opciones =>
        {
            var sesionValida = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RequisitoSinCambioPendiente())
                .Build();

            opciones.DefaultPolicy = sesionValida;
            opciones.FallbackPolicy = sesionValida;

            foreach (var (codigo, _) in CodigoPermiso.Todos)
            {
                opciones.AddPolicy(codigo, politica => politica
                    .Combine(sesionValida)
                    .RequireClaim(ConfiguracionJwt.ClaimPermiso, codigo));
            }
        });

        return services;
    }
}
