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
    public async Task Si_otro_inicio_de_sesion_simultaneo_modifico_al_usuario_reintenta_sobre_el_estado_actual()
    {
        await _e.AgregarUsuarioAsync("med01", Contrasena, CodigoRol.Medico);
        var conConflictos = new RepositorioConConflictos(_e.Usuarios, conflictos: 2);
        var casoUso = new IniciarSesionCasoUso(conConflictos, _e.Eventos, _e.Hasher, _e.Emisor, _e.Reloj);

        var sesion = await casoUso.EjecutarAsync(new IniciarSesionComando("med01", Contrasena));

        Assert.NotNull(sesion);
        Assert.Equal(2, conConflictos.Recargas);
        Assert.Single(_e.Eventos.Eventos, e => e.Tipo == TipoEventoSeguridad.InicioSesionExitoso);
    }

    [Fact]
    public async Task Si_los_conflictos_persisten_se_rinde_despues_de_10_intentos()
    {
        await _e.AgregarUsuarioAsync("med01", Contrasena, CodigoRol.Medico);
        var conConflictos = new RepositorioConConflictos(_e.Usuarios, conflictos: 99);
        var casoUso = new IniciarSesionCasoUso(conConflictos, _e.Eventos, _e.Hasher, _e.Emisor, _e.Reloj);

        await Assert.ThrowsAsync<Sigh.Compartido.Errores.ConflictoError>(() =>
            casoUso.EjecutarAsync(new IniciarSesionComando("med01", Contrasena)));
        Assert.Equal(9, conConflictos.Recargas);
    }

    [Fact]
    public async Task Los_reintentos_no_vuelven_a_calcular_el_hash_de_la_contrasena()
    {
        await _e.AgregarUsuarioAsync("med01", Contrasena, CodigoRol.Medico);
        var casoUso = new IniciarSesionCasoUso(new RepositorioConConflictos(_e.Usuarios, conflictos: 3),
            _e.Eventos, _e.Hasher, _e.Emisor, _e.Reloj);

        await casoUso.EjecutarAsync(new IniciarSesionComando("med01", Contrasena));

        Assert.Equal(1, _e.Hasher.Verificaciones);
    }

    /// <summary>Simula que otra petición guardó antes: las primeras N actualizaciones fallan por concurrencia.</summary>
    private sealed class RepositorioConConflictos(Sigh.Administracion.Dominio.Contratos.IRepositorioUsuarios interno, int conflictos)
        : Sigh.Administracion.Dominio.Contratos.IRepositorioUsuarios
    {
        private int _pendientes = conflictos;
        public int Recargas { get; private set; }

        public Task ActualizarAsync(Sigh.Administracion.Dominio.Usuarios.Usuario usuario, CancellationToken ct = default) =>
            _pendientes-- > 0 ? throw new Sigh.Compartido.Errores.ConflictoError("conflicto") : interno.ActualizarAsync(usuario, ct);

        public Task RecargarAsync(Sigh.Administracion.Dominio.Usuarios.Usuario usuario, CancellationToken ct = default)
        {
            Recargas++;
            return interno.RecargarAsync(usuario, ct);
        }

        public Task<Sigh.Administracion.Dominio.Usuarios.Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) => interno.ObtenerPorIdAsync(id, ct);
        public Task<Sigh.Administracion.Dominio.Usuarios.Usuario?> ObtenerPorNombreUsuarioAsync(string n, CancellationToken ct = default) => interno.ObtenerPorNombreUsuarioAsync(n, ct);
        public Task<bool> ExisteAlgunoAsync(CancellationToken ct = default) => interno.ExisteAlgunoAsync(ct);
        public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct = default) => interno.ContarAdministradoresActivosAsync(ct);
        public Task<Sigh.Compartido.Paginacion.Pagina<Sigh.Administracion.Dominio.Usuarios.Usuario>> ListarAsync(
            Sigh.Administracion.Dominio.Contratos.FiltroUsuarios f, int p, int t, CancellationToken ct = default) => interno.ListarAsync(f, p, t, ct);
        public Task AgregarAsync(Sigh.Administracion.Dominio.Usuarios.Usuario usuario, CancellationToken ct = default) => interno.AgregarAsync(usuario, ct);
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
