using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Permisos;

/// <summary>GET /api/v1/roles/matriz-permisos (FR-013, historia 3, escenarios 4 y 5).</summary>
public sealed class MatrizPermisosControllerTests : IDisposable
{
    private const string Ruta = "/api/v1/roles/matriz-permisos";
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private HttpClient ComoAdministrador() => _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Administrador));

    [Fact]
    public async Task Devuelve_los_8_roles_y_los_13_permisos_iguales_a_la_matriz_fija()
    {
        var matriz = await ComoAdministrador().GetFromJsonAsync<JsonElement>(Ruta);

        var roles = matriz.GetProperty("roles").EnumerateArray().ToList();
        Assert.Equal(Enum.GetNames<CodigoRol>(), roles.Select(r => r.GetProperty("codigo").GetString()));
        Assert.All(roles, r => Assert.False(string.IsNullOrWhiteSpace(r.GetProperty("nombre").GetString())));

        var permisos = matriz.GetProperty("permisos").EnumerateArray().ToList();
        Assert.Equal(13, permisos.Count);
        foreach (var (codigo, descripcion, rolesEsperados) in MatrizPermisos.Filas())
        {
            var fila = permisos.Single(p => p.GetProperty("codigo").GetString() == codigo);
            Assert.Equal(descripcion, fila.GetProperty("descripcion").GetString());
            Assert.Equal(rolesEsperados.Select(r => r.ToString()),
                fila.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        }
    }

    [Fact]
    public async Task No_existen_operaciones_para_crear_editar_ni_eliminar_roles_o_permisos()
    {
        var cliente = ComoAdministrador();

        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.PostAsJsonAsync(Ruta, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.PutAsJsonAsync(Ruta, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.DeleteAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsJsonAsync("/api/v1/roles", new { codigo = "Nuevo" })).StatusCode);
    }

    [Theory]
    [InlineData(CodigoRol.Medico)]
    [InlineData(CodigoRol.Directivo)]
    public async Task Sin_el_permiso_usuarios_gestionar_responde_403(CodigoRol rol)
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(rol)).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
