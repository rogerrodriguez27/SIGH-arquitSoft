using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sigh.Administracion.Aplicacion.Autenticacion.IniciarSesion;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura;
using Sigh.Administracion.Infraestructura.Seguridad;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Sigh.Compartido.Tiempo;
using Xunit.Abstractions;

namespace Sigh.Administracion.Integracion.Tests.Autenticacion;

/// <summary>
/// SC-007 (EQ-01): con 100 usuarios iniciando sesión a la vez, el 95 % de los inicios responde en 2 s o
/// menos. Usa los adaptadores reales (PostgreSQL, PasswordHasher y JWT) registrados como en la API.
/// </summary>
[Trait("Category", "Integracion")]
public sealed class InicioSesionCargaTests : IClassFixture<PostgresFixture>
{
    private const int Usuarios = 100;
    private const string Contrasena = "Clave2026";
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(2);
    private readonly PostgresFixture _bd;
    private readonly ITestOutputHelper _salida;

    public InicioSesionCargaTests(PostgresFixture bd, ITestOutputHelper salida)
    {
        _bd = bd;
        _salida = salida;
    }

    /// <summary>Mismo registro de dependencias que Sigh.Api, con una clave JWT generada en la prueba.</summary>
    private ServiceProvider ConstruirServicios()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SighOltp"] = _bd.CadenaConexion,
        }).Build();

        var services = new ServiceCollection();
        services.AgregarModuloAdministracion(configuracion);
        services.AddOptions<OpcionesJwt>().Configure(o =>
        {
            o.Emisor = "sigh-api";
            o.Audiencia = "sigh-web";
            o.ClaveFirma = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        });
        services.AddScoped<IniciarSesionCasoUso>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Cien_inicios_de_sesion_simultaneos_responden_en_2_segundos_o_menos_en_el_95_por_ciento()
    {
        await using var servicios = ConstruirServicios();
        var hasher = servicios.GetRequiredService<IHasherContrasenas>();
        var reloj = servicios.GetRequiredService<IReloj>();
        var nombres = Enumerable.Range(0, Usuarios).Select(i => $"carga{i:000}").ToList();

        await using (var ambito = servicios.CreateAsyncScope())
        {
            var repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioUsuarios>();
            foreach (var nombre in nombres)
            {
                var usuario = Usuario.Crear(nombre, "Usuario Ficticio", ContrasenaPropuesta.Crear(Contrasena),
                    [CodigoRol.Medico], hasher, reloj);
                await repositorio.AgregarAsync(usuario);
            }
        }

        var inicio = new TaskCompletionSource();
        var tareas = nombres.Select(async nombre =>
        {
            await inicio.Task;                                               // todos arrancan a la vez
            await using var ambito = servicios.CreateAsyncScope();          // una "petición" por usuario
            var casoUso = ambito.ServiceProvider.GetRequiredService<IniciarSesionCasoUso>();
            var cronometro = Stopwatch.StartNew();
            var sesion = await casoUso.EjecutarAsync(new IniciarSesionComando(nombre, Contrasena));
            cronometro.Stop();
            Assert.NotNull(sesion);
            return cronometro.Elapsed;
        }).ToList();
        inicio.SetResult();
        var tiempos = await Task.WhenAll(tareas);

        var p95 = tiempos.Order().ElementAt((int)Math.Ceiling(tiempos.Length * 0.95) - 1);
        _salida.WriteLine($"{Usuarios} inicios simultáneos: p95 = {p95.TotalMilliseconds:F0} ms, máximo = {tiempos.Max().TotalMilliseconds:F0} ms");
        Assert.True(p95 <= Limite, $"p95 = {p95.TotalMilliseconds:F0} ms (límite {Limite.TotalMilliseconds} ms)");
    }
}
