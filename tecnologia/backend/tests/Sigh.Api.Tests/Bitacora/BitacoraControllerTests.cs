using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Api.Tests.Soporte;
using Sigh.Compartido.Tiempo;

namespace Sigh.Api.Tests.Bitacora;

/// <summary>GET /api/v1/bitacora-accesos (HU16, FR-023 a FR-025, contrato PaginaBitacora).</summary>
public sealed class BitacoraControllerTests : IDisposable
{
    private const string Ruta = "/api/v1/bitacora-accesos";
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private static string Rango(TimeSpan duracion)
    {
        var hasta = DateTimeOffset.UtcNow.AddMinutes(1);
        return $"desde={Uri.EscapeDataString((hasta - duracion).ToString("O"))}&hasta={Uri.EscapeDataString(hasta.ToString("O"))}";
    }

    private async Task PrecargarAsync()
    {
        await _fabrica.Bitacora.AgregarAsync(EntradaBitacora.Crear(Guid.NewGuid(), "med01", CodigoRol.Medico,
            AccionBitacora.ConsultarHistoria, Guid.NewGuid(), ModuloOrigen.Hce, ResultadoAcceso.Permitido, new RelojSistema()));
        await _fabrica.Bitacora.AgregarEnTransaccionPropiaAsync(EntradaBitacora.Crear(Guid.NewGuid(), "farm01",
            CodigoRol.Farmaceutico, AccionBitacora.ConsultarHistoria, Guid.NewGuid(), ModuloOrigen.Hce,
            ResultadoAcceso.Denegado, new RelojSistema()));
    }

    [Fact]
    public async Task El_administrador_obtiene_la_pagina_con_exactamente_los_campos_del_contrato()
    {
        await PrecargarAsync();
        var adminId = Guid.NewGuid();

        var pagina = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaUsuario(adminId, CodigoRol.Administrador))
            .GetFromJsonAsync<JsonElement>($"{Ruta}?{Rango(TimeSpan.FromDays(1))}");

        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(20, pagina.GetProperty("tamano").GetInt32());
        var campos = pagina.GetProperty("elementos")[0].EnumerateObject().Select(p => p.Name).Order();
        Assert.Equal(["accion", "fechaHora", "historiaId", "id", "moduloOrigen", "nombreUsuario", "resultado", "rol", "usuarioId"], campos);
        Assert.Contains(pagina.GetProperty("elementos").EnumerateArray(), e => e.GetProperty("resultado").GetString() == "Denegado");

        var evento = Assert.Single(_fabrica.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.BitacoraConsultada);
        Assert.Equal(adminId, evento.ActorId);
    }

    [Fact]
    public async Task Filtra_por_resultado()
    {
        await PrecargarAsync();

        var pagina = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Administrador))
            .GetFromJsonAsync<JsonElement>($"{Ruta}?{Rango(TimeSpan.FromDays(1))}&resultado=Denegado");

        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
        Assert.Equal("farm01", pagina.GetProperty("elementos")[0].GetProperty("nombreUsuario").GetString());
    }

    [Theory]
    [InlineData(CodigoRol.Medico)]
    [InlineData(CodigoRol.Epidemiologo)]
    [InlineData(CodigoRol.Directivo)]
    [InlineData(CodigoRol.Farmaceutico)]
    public async Task Los_demas_roles_reciben_403(CodigoRol rol)
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(rol))
            .GetAsync($"{Ruta}?{Rango(TimeSpan.FromDays(1))}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.DoesNotContain(_fabrica.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.BitacoraConsultada);
    }

    [Fact]
    public async Task Un_rango_de_mas_de_un_anio_responde_422()
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Administrador))
            .GetAsync($"{Ruta}?{Rango(TimeSpan.FromDays(730))}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Un_rango_invertido_responde_422()
    {
        var ahora = DateTimeOffset.UtcNow;
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Administrador))
            .GetAsync($"{Ruta}?desde={Uri.EscapeDataString(ahora.ToString("O"))}&hasta={Uri.EscapeDataString(ahora.AddDays(-1).ToString("O"))}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_el_rango_de_fechas_responde_400()
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Administrador)).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task No_existen_operaciones_para_modificar_o_eliminar_la_bitacora()
    {
        var cliente = _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Administrador));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.DeleteAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.PutAsJsonAsync(Ruta, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await cliente.PostAsJsonAsync(Ruta, new { })).StatusCode);
    }
}
