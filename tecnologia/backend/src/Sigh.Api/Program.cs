// Raíz de composición del monolito modular (docs §6.5).
using System.Diagnostics;
using System.Text.Json.Serialization;
using Sigh.Administracion.Aplicacion.AdministradorInicial;
using Sigh.Administracion.Aplicacion.Autenticacion.CambiarContrasena;
using Sigh.Administracion.Aplicacion.Autenticacion.IniciarSesion;
using Sigh.Administracion.Infraestructura;
using Sigh.Api.Arranque;
using Sigh.Api.Autorizacion;
using Sigh.Api.Configuracion;
using Sigh.Api.Middlewares;
using Sigh.Api.Salud;

var builder = WebApplication.CreateBuilder(args);

// Módulos
builder.Services.AgregarModuloAdministracion(builder.Configuration);   // adaptadores del módulo Administración
builder.Services.AddScoped<IniciarSesionCasoUso>();                    // casos de uso del módulo Administración
builder.Services.AddScoped<CambiarContrasenaCasoUso>();
builder.Services.AddScoped<CrearAdministradorInicialCasoUso>();
// builder.Services.AgregarModuloHce(builder.Configuration);           ... resto de módulos

// Seguridad: JWT sin estado y una política por permiso (ADR-007, DA07)
builder.Services.AgregarAutenticacionJwt(builder.Configuration);
builder.Services.AgregarAutorizacionPorPermisos();

// Errores controlados en application/problem+json (principio III)
builder.Services.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = contexto =>
    contexto.ProblemDetails.Extensions["traceId"] =
        Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ManejadorErrores>();

// CORS: solo el origen del frontend del SIGH (principio III)
const string PoliticaCors = "FrontendSigh";
builder.Services.AddCors(opciones => opciones.AddPolicy(PoliticaCors, politica =>
{
    var origen = builder.Configuration["Cors:OrigenPermitido"];
    if (!string.IsNullOrWhiteSpace(origen))
        politica.WithOrigins(origen).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Sigh.Administracion.Api.Controladores.AutenticacionController).Assembly)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHealthChecks().AddCheck<VerificacionBaseDatos>("base-datos");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/salud").AllowAnonymous();

if (app.Configuration.GetValue("BaseDatos:AplicarMigraciones", true))
{
    await app.AplicarMigracionesAsync();
}
await app.CrearAdministradorInicialAsync();

await app.RunAsync();

/// <summary>Visible para WebApplicationFactory en las pruebas.</summary>
public partial class Program;
