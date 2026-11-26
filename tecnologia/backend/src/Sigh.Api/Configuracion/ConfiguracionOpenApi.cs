using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Sigh.Api.Configuracion;

/// <summary>
/// Documento OpenAPI generado desde los controladores (/openapi/v1.json) y Swagger UI (/swagger).
/// Declara el esquema Bearer (JWT) y lo exige en todas las operaciones salvo las anónimas (principio III).
/// </summary>
public static class ConfiguracionOpenApi
{
    public const string RutaDocumento = "/openapi/v1.json";
    private const string Esquema = "bearer";

    public static IServiceCollection AgregarDocumentacionOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", opciones =>
        {
            opciones.AddDocumentTransformer((documento, _, _) =>
            {
                documento.Info = new OpenApiInfo
                {
                    Title = "SIGH – API del módulo Administración",
                    Version = "v1",
                    Description = "Autenticación, usuarios, roles, matriz de permisos y bitácora de accesos a la HCE " +
                        "(spec 001-admin-auth-rbac). Todas las rutas exigen un JWT (Bearer) salvo iniciar-sesion. " +
                        "Los errores usan application/problem+json.",
                };
                documento.Components ??= new OpenApiComponents();
                documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                documento.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = Esquema,
                    BearerFormat = "JWT",
                    Description = "Token obtenido en POST /api/v1/autenticacion/iniciar-sesion (30 minutos).",
                };
                return Task.CompletedTask;
            });

            opciones.AddOperationTransformer((operacion, contexto, _) =>
            {
                var anonimo = contexto.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
                if (!anonimo)
                {
                    operacion.Security ??= [];
                    operacion.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(Esquema, contexto.Document)] = [],
                    });
                }
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>La documentación se puede ver sin sesión; los endpoints que describe siguen protegidos.</summary>
    public static WebApplication UsarDocumentacionOpenApi(this WebApplication app)
    {
        app.MapOpenApi(RutaDocumento).AllowAnonymous();
        app.UseSwaggerUI(opciones =>
        {
            opciones.SwaggerEndpoint(RutaDocumento, "SIGH v1");
            opciones.RoutePrefix = "swagger";
            opciones.DocumentTitle = "SIGH – API";
        });
        return app;
    }
}
