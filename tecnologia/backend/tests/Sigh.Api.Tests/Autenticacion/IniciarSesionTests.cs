using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Autenticacion;

/// <summary>POST /api/v1/autenticacion/iniciar-sesion (FR-001 a FR-004, contrato SesionRespuesta).</summary>
public sealed class IniciarSesionTests : IDisposable
{
    private const string Ruta = "/api/v1/autenticacion/iniciar-sesion";
    private const string Contrasena = "Clave2026";
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private Task<HttpResponseMessage> Iniciar(string usuario, string contrasena) =>
        _fabrica.CreateClient().PostAsJsonAsync(Ruta, new { nombreUsuario = usuario, contrasena });

    [Fact]
    public async Task Con_credenciales_validas_responde_200_con_token_de_30_minutos_y_sus_claims()
    {
        var usuario = await _fabrica.CrearUsuarioAsync("med01", Contrasena, [CodigoRol.Medico]);

        var respuesta = await Iniciar("MED01 ", Contrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(cuerpo.GetProperty("token").GetString());

        Assert.Equal(usuario.Id.ToString(), jwt.Subject);
        Assert.Equal("med01", jwt.GetClaim("name").Value);
        Assert.Equal(["Medico"], jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value));
        Assert.Equal(MatrizPermisos.PermisosDe([CodigoRol.Medico]).Order(),
            jwt.Claims.Where(c => c.Type == "perm").Select(c => c.Value).Order());
        Assert.False(jwt.TryGetClaim("cambio_contrasena_pendiente", out _));
        Assert.Equal(TimeSpan.FromMinutes(30), jwt.ValidTo - jwt.IssuedAt);

        var datos = cuerpo.GetProperty("usuario");
        Assert.Equal("med01", datos.GetProperty("nombreUsuario").GetString());
        Assert.False(datos.GetProperty("cambioContrasenaPendiente").GetBoolean());
        Assert.True(cuerpo.TryGetProperty("expiraEn", out _));
    }

    [Fact]
    public async Task Con_cambio_pendiente_el_token_lo_indica()
    {
        await _fabrica.CrearUsuarioAsync("nuevo01", Contrasena, [CodigoRol.Directivo], cambioPendiente: true);

        var cuerpo = await (await Iniciar("nuevo01", Contrasena)).Content.ReadFromJsonAsync<JsonElement>();
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(cuerpo.GetProperty("token").GetString());

        Assert.Equal("true", jwt.GetClaim("cambio_contrasena_pendiente").Value);
        Assert.True(cuerpo.GetProperty("usuario").GetProperty("cambioContrasenaPendiente").GetBoolean());
    }

    [Fact]
    public async Task Todos_los_rechazos_responden_401_con_el_mismo_mensaje_generico()
    {
        await _fabrica.CrearUsuarioAsync("farm01", Contrasena, [CodigoRol.Farmaceutico]);
        var desactivado = await _fabrica.CrearUsuarioAsync("lab01", Contrasena, [CodigoRol.PersonalLaboratorio]);
        typeof(Usuario).GetProperty(nameof(Usuario.Estado))!.SetValue(desactivado, EstadoUsuario.Desactivado);
        await _fabrica.CrearUsuarioAsync("bloq01", Contrasena, [CodigoRol.Medico]);
        for (var i = 0; i < 5; i++) await Iniciar("bloq01", "Incorrecta1");

        var respuestas = new[]
        {
            await Iniciar("farm01", "Incorrecta1"),     // contraseña incorrecta
            await Iniciar("noexiste", Contrasena),      // usuario inexistente
            await Iniciar("lab01", Contrasena),         // usuario desactivado
            await Iniciar("bloq01", Contrasena),        // cuenta bloqueada, contraseña correcta
        };

        var titulos = new List<string?>();
        foreach (var respuesta in respuestas)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
            Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
            var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            titulos.Add(problema.GetProperty("title").GetString());
            Assert.False(problema.TryGetProperty("token", out _));
        }

        Assert.All(titulos, t => Assert.Equal("Usuario o contraseña incorrectos", t));
    }

    [Theory]
    [InlineData("{\"contrasena\":\"Clave2026\"}")]
    [InlineData("{\"nombreUsuario\":\"med01\"}")]
    public async Task Sin_datos_obligatorios_responde_400(string json)
    {
        var respuesta = await _fabrica.CreateClient().PostAsync(Ruta,
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
