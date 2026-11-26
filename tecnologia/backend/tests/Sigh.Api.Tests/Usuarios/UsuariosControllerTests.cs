using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Usuarios;

/// <summary>/api/v1/usuarios (HU14, FR-008 a FR-010, FR-014, FR-015, contrato api-administracion).</summary>
public sealed class UsuariosControllerTests : IDisposable
{
    private const string Ruta = "/api/v1/usuarios";
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    /// <summary>Cliente con el token real del administrador, obtenido al iniciar sesión.</summary>
    private async Task<(HttpClient Cliente, Usuario Admin)> ComoAdministradorAsync()
    {
        var admin = await _fabrica.CrearUsuarioAsync("admin", "Clave2026", [CodigoRol.Administrador]);
        var sesion = await (await _fabrica.CreateClient().PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new { nombreUsuario = "admin", contrasena = "Clave2026" })).Content.ReadFromJsonAsync<JsonElement>();
        return (_fabrica.ClienteConToken(sesion.GetProperty("token").GetString()!), admin);
    }

    private static object NuevoFarmaceutico(string nombreUsuario = "farm01") => new
    {
        nombreUsuario,
        nombreCompleto = "Farmacéutico Ficticio",
        contrasenaInicial = "Inicial2026",
        roles = new[] { "Farmaceutico" },
    };

    [Fact]
    public async Task Crear_responde_201_con_Location_y_sin_datos_de_la_contrasena()
    {
        var (cliente, _) = await ComoAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync(Ruta, NuevoFarmaceutico());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var usuario = JsonDocument.Parse(texto).RootElement;
        Assert.Equal($"/api/v1/usuarios/{usuario.GetProperty("id").GetString()}", respuesta.Headers.Location?.OriginalString);
        Assert.Equal("Activo", usuario.GetProperty("estado").GetString());
        Assert.Equal("Farmaceutico", usuario.GetProperty("roles")[0].GetString());
        Assert.True(usuario.GetProperty("cambioContrasenaPendiente").GetBoolean());
        Assert.False(usuario.GetProperty("bloqueado").GetBoolean());
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contrasena\"", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Inicial2026", texto);
    }

    [Fact]
    public async Task Un_usuario_creado_puede_iniciar_sesion_y_recibe_403_al_gestionar_usuarios()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        await cliente.PostAsJsonAsync(Ruta, NuevoFarmaceutico());
        var anonimo = _fabrica.CreateClient();
        var sesion = await (await anonimo.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new { nombreUsuario = "farm01", contrasena = "Inicial2026" })).Content.ReadFromJsonAsync<JsonElement>();
        var farm = _fabrica.ClienteConToken(sesion.GetProperty("token").GetString()!);
        var cambio = await (await farm.PostAsJsonAsync("/api/v1/autenticacion/cambiar-contrasena",
            new { contrasenaActual = "Inicial2026", contrasenaNueva = "Propia2026" })).Content.ReadFromJsonAsync<JsonElement>();
        var farmConSesionNueva = _fabrica.ClienteConToken(cambio.GetProperty("token").GetString()!);

        var respuesta = await farmConSesionNueva.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_un_duplicado_normalizado_responde_409()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        await cliente.PostAsJsonAsync(Ruta, NuevoFarmaceutico());

        var respuesta = await cliente.PostAsJsonAsync(Ruta, NuevoFarmaceutico("FARM01 "));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crear_sin_roles_o_con_un_rol_inexistente_responde_400()
    {
        var (cliente, _) = await ComoAdministradorAsync();

        var sinRoles = await cliente.PostAsJsonAsync(Ruta, new
        {
            nombreUsuario = "farm01", nombreCompleto = "Farmacéutico Ficticio", contrasenaInicial = "Inicial2026",
            roles = Array.Empty<string>(),
        });
        var rolInexistente = await cliente.PostAsJsonAsync(Ruta, new
        {
            nombreUsuario = "farm01", nombreCompleto = "Farmacéutico Ficticio", contrasenaInicial = "Inicial2026",
            roles = new[] { "Superusuario" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, sinRoles.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rolInexistente.StatusCode);
    }

    [Fact]
    public async Task Listar_filtra_y_pagina()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        await _fabrica.CrearUsuarioAsync("med01", "Clave2026", [CodigoRol.Medico]);
        await _fabrica.CrearUsuarioAsync("med02", "Clave2026", [CodigoRol.Medico]);
        await _fabrica.CrearUsuarioAsync("farm01", "Clave2026", [CodigoRol.Farmaceutico]);

        var pagina = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}?rol=Medico&estado=Activo&pagina=1&tamano=1");

        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        Assert.Equal(1, pagina.GetProperty("elementos").GetArrayLength());
        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(1, pagina.GetProperty("tamano").GetInt32());
    }

    [Fact]
    public async Task Listar_con_un_tamano_mayor_a_100_responde_400()
    {
        var (cliente, _) = await ComoAdministradorAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.GetAsync($"{Ruta}?tamano=101")).StatusCode);
    }

    [Fact]
    public async Task Consultar_editar_y_cambiar_roles()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        var medico = await _fabrica.CrearUsuarioAsync("med01", "Clave2026", [CodigoRol.Medico]);

        var consultado = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{medico.Id}");
        var editado = await cliente.PutAsJsonAsync($"{Ruta}/{medico.Id}", new { nombreCompleto = "Nombre Corregido" });
        var roles = await cliente.PutAsJsonAsync($"{Ruta}/{medico.Id}/roles", new { roles = new[] { "Medico", "Epidemiologo" } });

        Assert.Equal("med01", consultado.GetProperty("nombreUsuario").GetString());
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        Assert.Equal("Nombre Corregido", medico.NombreCompleto);
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        Assert.Equal([CodigoRol.Medico, CodigoRol.Epidemiologo], medico.Roles.Order());
    }

    [Fact]
    public async Task Un_usuario_inexistente_responde_404()
    {
        var (cliente, _) = await ComoAdministradorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"{Ruta}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync($"{Ruta}/{Guid.NewGuid()}/desactivar", null)).StatusCode);
    }

    [Fact]
    public async Task Desactivar_y_reactivar_responden_204()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        var medico = await _fabrica.CrearUsuarioAsync("med01", "Clave2026", [CodigoRol.Medico]);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync($"{Ruta}/{medico.Id}/desactivar", null)).StatusCode);
        Assert.Equal(EstadoUsuario.Desactivado, medico.Estado);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync($"{Ruta}/{medico.Id}/reactivar", null)).StatusCode);
        Assert.Equal(EstadoUsuario.Activo, medico.Estado);
    }

    [Fact]
    public async Task Desactivarse_a_si_mismo_o_quitarse_el_rol_responde_422()
    {
        var (cliente, admin) = await ComoAdministradorAsync();

        var desactivar = await cliente.PostAsync($"{Ruta}/{admin.Id}/desactivar", null);
        var quitarRol = await cliente.PutAsJsonAsync($"{Ruta}/{admin.Id}/roles", new { roles = new[] { "Directivo" } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, desactivar.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, quitarRol.StatusCode);
        Assert.Equal(EstadoUsuario.Activo, admin.Estado);
    }

    [Fact]
    public async Task Restablecer_contrasena_responde_204_y_exige_cambiarla()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        var medico = await _fabrica.CrearUsuarioAsync("med01", "Clave2026", [CodigoRol.Medico]);

        var respuesta = await cliente.PostAsJsonAsync($"{Ruta}/{medico.Id}/restablecer-contrasena",
            new { contrasenaTemporal = "Temporal2026" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.True(medico.CambioContrasenaPendiente);
    }

    [Fact]
    public async Task No_existe_DELETE()
    {
        var (cliente, _) = await ComoAdministradorAsync();
        var medico = await _fabrica.CrearUsuarioAsync("med01", "Clave2026", [CodigoRol.Medico]);

        var respuesta = await cliente.DeleteAsync($"{Ruta}/{medico.Id}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
        Assert.NotNull(_fabrica.Usuarios.Usuarios.SingleOrDefault(u => u.Id == medico.Id));
    }

    [Theory]
    [InlineData(CodigoRol.Medico)]
    [InlineData(CodigoRol.Farmaceutico)]
    [InlineData(CodigoRol.Directivo)]
    public async Task Sin_el_permiso_usuarios_gestionar_responde_403(CodigoRol rol)
    {
        var cliente = _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(rol));

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(Ruta, NuevoFarmaceutico())).StatusCode);
    }
}
