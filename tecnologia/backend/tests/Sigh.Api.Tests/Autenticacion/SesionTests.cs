using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Autenticacion;

/// <summary>Validez de la sesión y cambio de contraseña obligatorio (FR-002, FR-006, FR-007, SC-001).</summary>
public sealed class SesionTests : IDisposable
{
    private const string Protegido = "/api/v1/pruebas/protegido";
    private const string CerrarSesion = "/api/v1/autenticacion/cerrar-sesion";
    private const string CambiarContrasena = "/api/v1/autenticacion/cambiar-contrasena";
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    [Fact]
    public async Task Sin_token_responde_401_en_problem_json()
    {
        var cliente = _fabrica.CreateClient();

        var protegido = await cliente.GetAsync(Protegido);
        var cerrar = await cliente.PostAsync(CerrarSesion, null);

        Assert.Equal(HttpStatusCode.Unauthorized, protegido.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cerrar.StatusCode);
        Assert.Equal("application/problem+json", protegido.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Con_firma_invalida_responde_401()
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ConFirmaInvalida(CodigoRol.Medico))
            .GetAsync(Protegido);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_token_vencido_responde_401()
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.Vencido(CodigoRol.Medico))
            .GetAsync(Protegido);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_token_manipulado_responde_401()
    {
        var token = _fabrica.Tokens.ParaRoles(CodigoRol.Farmaceutico);
        var manipulado = token[..^4] + (token[^4..] == "AAAA" ? "BBBB" : "AAAA");

        var respuesta = await _fabrica.ClienteConToken(manipulado).GetAsync(Protegido);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_token_valido_permite_el_acceso()
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Medico))
            .GetAsync(Protegido);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_cambio_pendiente_solo_permite_cambiar_contrasena_y_cerrar_sesion()
    {
        var cliente = _fabrica.ClienteConToken(_fabrica.Tokens.ConCambioPendiente(CodigoRol.Administrador));

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(Protegido)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync(CerrarSesion, null)).StatusCode);
    }

    [Fact]
    public async Task El_cambio_obligatorio_entrega_una_sesion_nueva_que_ya_permite_el_acceso()
    {
        await _fabrica.CrearUsuarioAsync("admin", "Inicial2026", [CodigoRol.Administrador], cambioPendiente: true);
        var cliente = _fabrica.CreateClient();
        var inicio = await (await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new { nombreUsuario = "admin", contrasena = "Inicial2026" })).Content.ReadFromJsonAsync<JsonElement>();
        var conPendiente = _fabrica.ClienteConToken(inicio.GetProperty("token").GetString()!);

        Assert.Equal(HttpStatusCode.Forbidden, (await conPendiente.GetAsync(Protegido)).StatusCode);

        var cambio = await conPendiente.PostAsJsonAsync(CambiarContrasena,
            new { contrasenaActual = "Inicial2026", contrasenaNueva = "Nueva2026x" });
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var nueva = await cambio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(nueva.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean());

        var conSesionNueva = _fabrica.ClienteConToken(nueva.GetProperty("token").GetString()!);
        Assert.Equal(HttpStatusCode.OK, (await conSesionNueva.GetAsync(Protegido)).StatusCode);
    }

    [Fact]
    public async Task Cambiar_contrasena_con_la_actual_incorrecta_responde_422()
    {
        var usuario = await _fabrica.CrearUsuarioAsync("med01", "Clave2026", [CodigoRol.Medico]);
        var login = await (await _fabrica.CreateClient().PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new { nombreUsuario = usuario.NombreUsuario, contrasena = "Clave2026" })).Content.ReadFromJsonAsync<JsonElement>();

        var respuesta = await _fabrica.ClienteConToken(login.GetProperty("token").GetString()!)
            .PostAsJsonAsync(CambiarContrasena, new { contrasenaActual = "Otra2026x", contrasenaNueva = "Nueva2026x" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }
}
