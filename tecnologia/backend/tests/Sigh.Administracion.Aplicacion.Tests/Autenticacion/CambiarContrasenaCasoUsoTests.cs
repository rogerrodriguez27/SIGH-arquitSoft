using Sigh.Administracion.Aplicacion.Autenticacion.CambiarContrasena;
using Sigh.Administracion.Aplicacion.Tests.Dobles;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Aplicacion.Tests.Autenticacion;

/// <summary>CambiarContrasenaCasoUso (FR-005, FR-006, FR-026).</summary>
public sealed class CambiarContrasenaCasoUsoTests
{
    private readonly Escenario _e = new();

    private CambiarContrasenaCasoUso CasoUso() => new(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Emisor, _e.Reloj);

    [Fact]
    public async Task Cambia_la_contrasena_quita_el_cambio_pendiente_y_entrega_una_sesion_nueva()
    {
        var usuario = await _e.AgregarUsuarioAsync("med01", "Inicial2026", CodigoRol.Medico);

        var sesion = await CasoUso().EjecutarAsync(new CambiarContrasenaComando(usuario.Id, "Inicial2026", "Nueva2026x"));

        Assert.False(sesion.CambioContrasenaPendiente);
        Assert.False(usuario.CambioContrasenaPendiente);
        Assert.Equal("hash:Nueva2026x", usuario.HashContrasena);
        var evento = Assert.Single(_e.Eventos.Eventos);
        Assert.Equal(TipoEventoSeguridad.ContrasenaCambiada, evento.Tipo);
        Assert.DoesNotContain("Nueva2026x", evento.Detalle);
    }

    [Fact]
    public async Task Rechaza_si_la_contrasena_actual_es_incorrecta()
    {
        var usuario = await _e.AgregarUsuarioAsync("med01", "Inicial2026", CodigoRol.Medico);

        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            CasoUso().EjecutarAsync(new CambiarContrasenaComando(usuario.Id, "Otra2026", "Nueva2026x")));
        Assert.Empty(_e.Eventos.Eventos);
    }

    [Fact]
    public async Task Rechaza_una_contrasena_nueva_que_no_cumple_la_politica()
    {
        var usuario = await _e.AgregarUsuarioAsync("med01", "Inicial2026", CodigoRol.Medico);

        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            CasoUso().EjecutarAsync(new CambiarContrasenaComando(usuario.Id, "Inicial2026", "sololetras")));
    }

    [Fact]
    public async Task Responde_no_encontrado_si_el_usuario_no_existe()
    {
        await Assert.ThrowsAsync<RecursoNoEncontradoError>(() =>
            CasoUso().EjecutarAsync(new CambiarContrasenaComando(Guid.NewGuid(), "Inicial2026", "Nueva2026x")));
    }
}
