using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sigh.Administracion.Infraestructura.Seguridad;

namespace Sigh.Api.Configuracion;

/// <summary>Autenticación con JWT HS256 (research D4, ADR-007, ADR-012).</summary>
public static class ConfiguracionJwt
{
    public const string ClaimPermiso = "perm";
    public const string ClaimCambioContrasenaPendiente = "cambio_contrasena_pendiente";

    public static IServiceCollection AgregarAutenticacionJwt(this IServiceCollection services,
        IConfiguration configuracion)
    {
        // La API no arranca si falta la clave o tiene menos de 32 bytes (ValidateOnStart).
        services.AddOptions<OpcionesJwt>()
            .Bind(configuracion.GetSection(OpcionesJwt.Seccion))
            .Validate(o => o.EsValida(),
                "Configuración JWT inválida: Jwt__ClaveFirma debe tener al menos 32 bytes, y deben existir emisor y audiencia.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OpcionesJwt>>((opciones, jwt) =>
            {
                var valores = jwt.Value;
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = valores.Emisor,
                    ValidateAudience = true,
                    ValidAudience = valores.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(valores.ClaveEnBytes()),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });

        return services;
    }
}
