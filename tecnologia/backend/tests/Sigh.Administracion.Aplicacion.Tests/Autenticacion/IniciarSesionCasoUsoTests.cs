using Sigh.Administracion.Aplicacion.Autenticacion.IniciarSesion;
using Sigh.Administracion.Aplicacion.Tests.Dobles;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;

namespace Sigh.Administracion.Aplicacion.Tests.Autenticacion;

/// <summary>IniciarSesionCasoUso (FR-001 a FR-004, FR-026, research D6).</summary>
public sealed class IniciarSesionCasoUsoTests
{
    private const string Contrasena = "Clave2026";
    private readonly Escenario _e = new();

    private IniciarSesionCasoUso CasoUso() => new(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Emisor, _e.Reloj);

    private Task<Sigh.Administracion.Aplicacion.Autenticacion.SesionResultado?> Iniciar(string usuario, string contrasena) =>
        CasoUso().EjecutarAsync(new IniciarSesionComando(usuario, contrasena));

    [Fact]
    public async Task Con_credenciales_validas_entrega_sesion_con_roles_y_permisos_de_la_matriz()
    {
        var medico = await _e.AgregarUsuarioAsync("med01", Contrasena, CodigoRol.Medico, CodigoRol.Epidemiologo);

        var sesion = await Iniciar("  MED01 ", Contrasena);

        Assert.NotNull(sesion);
        Assert.Equal(medico.Id, sesion.UsuarioId);
        Assert.Equal([CodigoRol.Medico, CodigoRol.Epidemiologo], sesion.Roles);
        Assert.Equal(MatrizPermisos.PermisosDe([CodigoRol.Medico, CodigoRol.Epidemiologo]).Order(), sesion.Permisos.Order());
        Assert.Equal(_e.Reloj.Ahora.AddMinutes(30), sesion.ExpiraEn);
        Assert.Contains(TipoEventoSeguridad.InicioSesionExitoso, _e.TiposDeEvento());
    }

    [Fact]
    public async Task Con_contrasena_incorrecta_rechaza_y_registra_el_intento_fallido()
    {
        var usuario = await _e.AgregarUsuarioAsync("farm01", Contrasena, CodigoRol.Farmaceutico);

        var sesion = await Iniciar("farm01", "Incorrecta1");

        Assert.Null(sesion);
        var evento = Assert.Single(_e.Eventos.Eventos);
        Assert.Equal(TipoEventoSeguridad.InicioSesionFallido, evento.Tipo);
        Assert.Equal(usuario.Id, evento.ActorId);
        Assert.Equal(1, usuario.IntentosFallidos);
    }

    [Fact]
    public async Task Con_usuario_inexistente_rechaza_calcula_un_hash_y_registra_sin_actor()
    {
        var sesion = await Iniciar("noexiste", Contrasena);

        Assert.Null(sesion);
        Assert.Equal(1, _e.Hasher.Verificaciones);
        var evento = Assert.Single(_e.Eventos.Eventos);
        Assert.Equal(TipoEventoSeguridad.InicioSesionFallido, evento.Tipo);
        Assert.Null(evento.ActorId);
    }

    [Fact]
    public async Task Con_usuario_desactivado_rechaza_aunque_la_contrasena_sea_correcta()
    {
        var usuario = await _e.AgregarUsuarioAsync("lab01", Contrasena, CodigoRol.PersonalLaboratorio);
        usuario.Desactivar();

        Assert.Null(await Iniciar("lab01", Contrasena));
    }

    [Fact]
    public async Task El_quinto_fallo_registra_CUENTA_BLOQUEADA_y_luego_rechaza_la_contrasena_correcta()
    {
        await _e.AgregarUsuarioAsync("farm01", Contrasena, CodigoRol.Farmaceutico);
        for (var i = 0; i < 5; i++) await Iniciar("farm01", "Incorrecta1");

        Assert.Contains(TipoEventoSeguridad.CuentaBloqueada, _e.TiposDeEvento());
        Assert.Null(await Iniciar("farm01", Contrasena));
    }

    [Fact]
    public async Task Ningun_evento_contiene_la_contrasena()
    {
        await _e.AgregarUsuarioAsync("farm01", Contrasena, CodigoRol.Farmaceutico);
        await Iniciar("farm01", "SecretoIncorrecto9");
        await Iniciar("farm01", Contrasena);

        Assert.All(_e.Eventos.Eventos, e =>
        {
            Assert.DoesNotContain("SecretoIncorrecto9", e.Detalle);
            Assert.DoesNotContain(Contrasena, e.Detalle);
        });
    }
}
