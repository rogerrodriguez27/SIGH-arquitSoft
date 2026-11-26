using System.Net;
using System.Text.Json;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Documentacion;

/// <summary>
/// Documentación OpenAPI generada desde los controladores (T115): coincide con las rutas del contrato
/// contracts/api-administracion.openapi.yaml y declara el esquema Bearer.
/// </summary>
public sealed class OpenApiTests(FabricaApiPruebasCompartidaDoc compartida) : IClassFixture<FabricaApiPruebasCompartidaDoc>
{
    private readonly FabricaApiPruebas _fabrica = compartida.Fabrica;

    private async Task<JsonElement> DocumentoAsync()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
    }

    public static TheoryData<string, string> OperacionesDelContrato => new()
    {
        { "/api/v1/autenticacion/iniciar-sesion", "post" },
        { "/api/v1/autenticacion/cerrar-sesion", "post" },
        { "/api/v1/autenticacion/cambiar-contrasena", "post" },
        { "/api/v1/usuarios", "get" },
        { "/api/v1/usuarios", "post" },
        { "/api/v1/usuarios/{id}", "get" },
        { "/api/v1/usuarios/{id}", "put" },
        { "/api/v1/usuarios/{id}/roles", "put" },
        { "/api/v1/usuarios/{id}/desactivar", "post" },
        { "/api/v1/usuarios/{id}/reactivar", "post" },
        { "/api/v1/usuarios/{id}/restablecer-contrasena", "post" },
        { "/api/v1/roles/matriz-permisos", "get" },
        { "/api/v1/bitacora-accesos", "get" },
    };

    [Theory]
    [MemberData(nameof(OperacionesDelContrato))]
    public async Task Documenta_cada_operacion_del_contrato_con_su_resumen(string ruta, string metodo)
    {
        var documento = await DocumentoAsync();

        var operacion = documento.GetProperty("paths").GetProperty(ruta).GetProperty(metodo);
        Assert.False(string.IsNullOrWhiteSpace(operacion.GetProperty("summary").GetString()));
        Assert.True(operacion.GetProperty("responses").EnumerateObject().Any());
    }

    [Fact]
    public async Task No_documenta_operaciones_de_eliminacion()
    {
        var documento = await DocumentoAsync();

        foreach (var ruta in documento.GetProperty("paths").EnumerateObject().Where(r => r.Name.StartsWith("/api/v1/", StringComparison.Ordinal)))
            Assert.False(ruta.Value.TryGetProperty("delete", out _), $"{ruta.Name} no debe tener DELETE (FR-009, FR-021)");
    }

    [Fact]
    public async Task Declara_el_esquema_Bearer_y_lo_exige_salvo_en_iniciar_sesion()
    {
        var documento = await DocumentoAsync();

        var esquema = documento.GetProperty("components").GetProperty("securitySchemes").GetProperty("bearer");
        Assert.Equal("http", esquema.GetProperty("type").GetString());
        Assert.Equal("JWT", esquema.GetProperty("bearerFormat").GetString());

        var rutas = documento.GetProperty("paths");
        Assert.False(rutas.GetProperty("/api/v1/autenticacion/iniciar-sesion").GetProperty("post").TryGetProperty("security", out _));
        Assert.True(rutas.GetProperty("/api/v1/usuarios").GetProperty("get").TryGetProperty("security", out _));
        Assert.True(rutas.GetProperty("/api/v1/bitacora-accesos").GetProperty("get").TryGetProperty("security", out _));
    }

    [Fact]
    public async Task Swagger_UI_se_puede_ver_sin_sesion()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("swagger", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class FabricaApiPruebasCompartidaDoc : IDisposable
{
    public FabricaApiPruebas Fabrica { get; } = new();

    public void Dispose() => Fabrica.Dispose();
}
