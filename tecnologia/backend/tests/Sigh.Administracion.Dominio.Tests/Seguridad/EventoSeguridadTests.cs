using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Tests.Dobles;

namespace Sigh.Administracion.Dominio.Tests.Seguridad;

public sealed class EventoSeguridadTests
{
    [Fact]
    public void Toma_la_hora_del_reloj_del_servidor()
    {
        var reloj = RelojFijo.Nuevo();

        var evento = EventoSeguridad.Crear(TipoEventoSeguridad.InicioSesionExitoso, reloj, Guid.NewGuid());

        Assert.Equal(reloj.Ahora, evento.FechaHora);
        Assert.NotEqual(Guid.Empty, evento.Id);
    }

    [Fact]
    public void Rechaza_un_detalle_de_mas_de_500_caracteres()
    {
        var detalle = new string('x', EventoSeguridad.LongitudMaximaDetalle + 1);

        Assert.Throws<ArgumentException>(() =>
            EventoSeguridad.Crear(TipoEventoSeguridad.UsuarioEditado, RelojFijo.Nuevo(), detalle: detalle));
    }

    [Theory]
    [InlineData(TipoEventoSeguridad.InicioSesionExitoso, "INICIO_SESION_EXITOSO")]
    [InlineData(TipoEventoSeguridad.CuentaBloqueada, "CUENTA_BLOQUEADA")]
    [InlineData(TipoEventoSeguridad.BitacoraConsultada, "BITACORA_CONSULTADA")]
    public void Usa_los_codigos_del_modelo_de_datos(TipoEventoSeguridad tipo, string codigo)
    {
        Assert.Equal(codigo, tipo.ACodigo());
        Assert.Equal(tipo, TipoEventoSeguridadExtensiones.DesdeCodigo(codigo));
    }

    [Fact]
    public void No_expone_metodos_de_modificacion()
    {
        var setters = typeof(EventoSeguridad).GetProperties().Where(p => p.SetMethod?.IsPublic == true);

        Assert.Empty(setters);
    }
}
