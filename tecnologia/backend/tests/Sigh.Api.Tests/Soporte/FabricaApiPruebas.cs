using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Compartido.Tiempo;

namespace Sigh.Api.Tests.Soporte;

/// <summary>
/// Host de la API con los repositorios reemplazados por dobles en memoria (research D10). La clave JWT
/// se genera en cada ejecución: no hay secretos en el código (principio VI).
/// </summary>
public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public const string Emisor = "sigh-api";
    public const string Audiencia = "sigh-web";

    public string ClaveFirma { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public RepositorioUsuariosEnMemoria Usuarios { get; } = new();
    public RepositorioEventosSeguridadEnMemoria Eventos { get; } = new();
    public TokensPrueba Tokens => new(ClaveFirma, Emisor, Audiencia);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");
        builder.ConfigureAppConfiguration(configuracion => configuracion.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:ClaveFirma"] = ClaveFirma,
                ["Jwt:Emisor"] = Emisor,
                ["Jwt:Audiencia"] = Audiencia,
                ["ConnectionStrings:SighOltp"] = "Host=localhost;Database=sin_uso_en_pruebas",
                ["BaseDatos:AplicarMigraciones"] = "false",
                ["AdminInicial:Usuario"] = null,
                ["AdminInicial:NombreCompleto"] = null,
                ["AdminInicial:Contrasena"] = null,
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRepositorioUsuarios>();
            services.RemoveAll<IRepositorioEventosSeguridad>();
            services.AddSingleton<IRepositorioUsuarios>(Usuarios);
            services.AddSingleton<IRepositorioEventosSeguridad>(Eventos);
            services.AddControllers().AddApplicationPart(typeof(FabricaApiPruebas).Assembly);
        });
    }

    /// <summary>Crea un usuario ficticio. Por defecto, sin cambio de contraseña pendiente.</summary>
    public async Task<Usuario> CrearUsuarioAsync(string nombreUsuario, string contrasena,
        CodigoRol[] roles, bool cambioPendiente = false)
    {
        var hasher = Services.GetRequiredService<IHasherContrasenas>();
        var reloj = Services.GetRequiredService<IReloj>();
        var inicial = cambioPendiente ? contrasena : "Temporal2026x";

        var usuario = Usuario.Crear(nombreUsuario, "Usuario Ficticio", ContrasenaPropuesta.Crear(inicial),
            roles, hasher, reloj);
        if (!cambioPendiente)
            usuario.CambiarContrasena(inicial, ContrasenaPropuesta.Crear(contrasena), hasher);

        await Usuarios.AgregarAsync(usuario);
        return usuario;
    }

    public HttpClient ClienteConToken(string token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}
