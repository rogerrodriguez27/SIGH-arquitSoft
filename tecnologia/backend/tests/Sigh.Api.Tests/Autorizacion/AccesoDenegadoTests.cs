using System.Net;
using System.Text.Json;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Autorizacion;

/// <summary>
/// Un rechazo por falta de permisos no expone información y queda registrado como ACCESO_DENEGADO
/// (historia 2, escenario 6; historia 3, escenario 2; EQ-04).
/// </summary>
public sealed class AccesoDenegadoTests : IDisposable
{
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    [Fact]
    public async Task Un_farmaceutico_que_intenta_consultar_la_HCE_recibe_403_sin_datos_y_queda_registrado()
    {
        var farmaceuticoId = Guid.NewGuid();
        var cliente = _fabrica.ClienteConToken(_fabrica.Tokens.ParaUsuario(farmaceuticoId, CodigoRol.Farmaceutico));

        var respuesta = await cliente.GetAsync($"/api/v1/pruebas/permisos/{CodigoPermiso.ConsultarHce}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
        Assert.All(problema.EnumerateObject(), p => Assert.Contains(p.Name, new[] { "type", "title", "status", "traceId" }));

        var evento = Assert.Single(_fabrica.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.AccesoDenegado);
        Assert.Equal(farmaceuticoId, evento.ActorId);
        Assert.Contains($"/api/v1/pruebas/permisos/{CodigoPermiso.ConsultarHce}", evento.Detalle);
        Assert.Contains(CodigoPermiso.ConsultarHce, evento.Detalle);
    }

    [Fact]
    public async Task Un_usuario_sin_rol_Administrador_que_intenta_gestionar_usuarios_queda_registrado()
    {
        var medicoId = Guid.NewGuid();

        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ParaUsuario(medicoId, CodigoRol.Medico))
            .GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var evento = Assert.Single(_fabrica.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.AccesoDenegado);
        Assert.Equal(medicoId, evento.ActorId);
        Assert.Contains("GET /api/v1/usuarios", evento.Detalle);
    }

    [Fact]
    public async Task El_bloqueo_por_cambio_de_contrasena_pendiente_tambien_queda_registrado()
    {
        var respuesta = await _fabrica.ClienteConToken(_fabrica.Tokens.ConCambioPendiente(CodigoRol.Administrador))
            .GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var evento = Assert.Single(_fabrica.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.AccesoDenegado);
        Assert.Contains("cambio de contraseña pendiente", evento.Detalle);
    }

    [Fact]
    public async Task Un_acceso_permitido_o_sin_sesion_no_registra_ACCESO_DENEGADO()
    {
        await _fabrica.ClienteConToken(_fabrica.Tokens.ParaRoles(CodigoRol.Medico))
            .GetAsync($"/api/v1/pruebas/permisos/{CodigoPermiso.ConsultarHce}");
        await _fabrica.CreateClient().GetAsync("/api/v1/usuarios");

        Assert.DoesNotContain(_fabrica.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.AccesoDenegado);
    }
}
