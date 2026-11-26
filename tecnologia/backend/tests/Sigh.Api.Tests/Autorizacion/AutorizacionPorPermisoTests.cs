using System.Net;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Autorizacion;

/// <summary>
/// SC-001 y EQ-04: para cada uno de los 8 roles y cada uno de los 13 permisos, la API permite solo lo
/// que concede la matriz fija y rechaza el 100 % de lo demás (FR-012, historia 3).
/// </summary>
public sealed class AutorizacionPorPermisoTests(FabricaApiPruebasCompartida compartida)
    : IClassFixture<FabricaApiPruebasCompartida>
{
    private readonly FabricaApiPruebas _fabrica = compartida.Fabrica;

    public static TheoryData<CodigoRol, string> RolesPorPermiso()
    {
        var datos = new TheoryData<CodigoRol, string>();
        foreach (var rol in Enum.GetValues<CodigoRol>())
            foreach (var (codigo, _) in CodigoPermiso.Todos)
                datos.Add(rol, codigo);
        return datos;
    }

    [Theory]
    [MemberData(nameof(RolesPorPermiso))]
    public async Task Cada_rol_obtiene_exactamente_lo_que_concede_la_matriz(CodigoRol rol, string permiso)
    {
        var esperado = MatrizPermisos.PermisosDe([rol]).Contains(permiso) ? HttpStatusCode.OK : HttpStatusCode.Forbidden;

        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(rol))
            .GetAsync($"/api/v1/pruebas/permisos/{permiso}");

        Assert.Equal(esperado, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(CodigoPermiso.ConsultarHce)]
    [InlineData(CodigoPermiso.ReportesEpidemiologicos)]
    [InlineData(CodigoPermiso.DashboardsGestion)]
    public async Task Un_usuario_con_dos_roles_puede_usar_lo_que_permite_cualquiera_de_ellos(string permiso)
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Medico, CodigoRol.Epidemiologo))
            .GetAsync($"/api/v1/pruebas/permisos/{permiso}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_sesion_ningun_permiso_es_accesible()
    {
        var cliente = _fabrica.CreateClient();

        foreach (var (codigo, _) in CodigoPermiso.Todos)
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync($"/api/v1/pruebas/permisos/{codigo}")).StatusCode);
    }
}

/// <summary>Un solo host para las 104 combinaciones, para que la prueba sea rápida.</summary>
public sealed class FabricaApiPruebasCompartida : IDisposable
{
    public FabricaApiPruebas Fabrica { get; } = new();

    public void Dispose() => Fabrica.Dispose();
}
