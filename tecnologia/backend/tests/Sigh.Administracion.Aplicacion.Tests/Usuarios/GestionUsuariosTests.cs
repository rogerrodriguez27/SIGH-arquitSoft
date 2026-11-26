using Sigh.Administracion.Aplicacion.Tests.Dobles;
using Sigh.Administracion.Aplicacion.Usuarios.CambiarRoles;
using Sigh.Administracion.Aplicacion.Usuarios.ConsultarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.CrearUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.DesactivarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.EditarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.ListarUsuarios;
using Sigh.Administracion.Aplicacion.Usuarios.ReactivarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.RestablecerContrasena;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Aplicacion.Tests.Usuarios;

/// <summary>Casos de uso de gestión de usuarios (HU14, FR-008 a FR-010, FR-014, FR-015, FR-017).</summary>
public sealed class GestionUsuariosTests
{
    private readonly Escenario _e = new();
    private Usuario _admin = null!;

    private async Task<Guid> PrepararAdminAsync()
    {
        _admin = await _e.AgregarUsuarioAsync("admin", "Clave2026", CodigoRol.Administrador);
        return _admin.Id;
    }

    private EventoSeguridad UltimoEvento() => _e.Eventos.Eventos[^1];

    // ---------- Crear ----------

    [Fact]
    public async Task Crear_registra_un_usuario_activo_con_cambio_pendiente_y_el_evento()
    {
        var actor = await PrepararAdminAsync();

        var creado = await new CrearUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Reloj).EjecutarAsync(
            new CrearUsuarioComando(actor, "farm01", "Farmacéutico Ficticio", "Inicial2026", [CodigoRol.Farmaceutico]));

        Assert.Equal(EstadoUsuario.Activo, creado.Estado);
        Assert.True(creado.CambioContrasenaPendiente);
        Assert.Equal([CodigoRol.Farmaceutico], creado.Roles);
        var evento = UltimoEvento();
        Assert.Equal(TipoEventoSeguridad.UsuarioCreado, evento.Tipo);
        Assert.Equal(actor, evento.ActorId);
        Assert.Equal(creado.Id, evento.ObjetivoId);
        Assert.DoesNotContain("Inicial2026", evento.Detalle);
    }

    [Fact]
    public async Task Crear_rechaza_un_nombre_de_usuario_duplicado_aunque_cambien_mayusculas_y_espacios()
    {
        var actor = await PrepararAdminAsync();
        var casoUso = new CrearUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Reloj);
        await casoUso.EjecutarAsync(new(actor, "farm01", "Farmacéutico Ficticio", "Inicial2026", [CodigoRol.Farmaceutico]));

        await Assert.ThrowsAsync<ConflictoError>(() => casoUso.EjecutarAsync(
            new(actor, "FARM01 ", "Otro Ficticio", "Inicial2026", [CodigoRol.Farmaceutico])));
    }

    [Fact]
    public async Task Crear_exige_al_menos_un_rol()
    {
        var actor = await PrepararAdminAsync();

        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            new CrearUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Reloj)
                .EjecutarAsync(new(actor, "farm01", "Farmacéutico Ficticio", "Inicial2026", [])));
    }

    // ---------- Editar ----------

    [Fact]
    public async Task Editar_cambia_el_nombre_y_registra_el_antes_y_el_despues()
    {
        var actor = await PrepararAdminAsync();
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);

        var editado = await new EditarUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj)
            .EjecutarAsync(new(actor, medico.Id, "Nombre Corregido"));

        Assert.Equal("Nombre Corregido", editado.NombreCompleto);
        Assert.Equal(TipoEventoSeguridad.UsuarioEditado, UltimoEvento().Tipo);
        Assert.Contains("→ Nombre Corregido", UltimoEvento().Detalle);
    }

    [Fact]
    public async Task Editar_un_usuario_inexistente_responde_no_encontrado()
    {
        var actor = await PrepararAdminAsync();

        await Assert.ThrowsAsync<RecursoNoEncontradoError>(() =>
            new EditarUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj).EjecutarAsync(new(actor, Guid.NewGuid(), "Nombre")));
    }

    // ---------- Roles ----------

    [Fact]
    public async Task Cambiar_roles_reemplaza_los_roles_y_registra_el_cambio()
    {
        var actor = await PrepararAdminAsync();
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);

        var actualizado = await new CambiarRolesCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj)
            .EjecutarAsync(new(actor, medico.Id, [CodigoRol.Medico, CodigoRol.Epidemiologo]));

        Assert.Equal([CodigoRol.Medico, CodigoRol.Epidemiologo], actualizado.Roles);
        Assert.Equal(TipoEventoSeguridad.RolesCambiados, UltimoEvento().Tipo);
        Assert.Equal("roles: Medico → Medico, Epidemiologo", UltimoEvento().Detalle);
    }

    [Fact]
    public async Task No_se_puede_quitar_el_rol_al_ultimo_administrador()
    {
        await PrepararAdminAsync();
        var otroActor = Guid.NewGuid();

        await Assert.ThrowsAsync<ReglaNegocioError>(() => new CambiarRolesCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj)
            .EjecutarAsync(new(otroActor, _admin.Id, [CodigoRol.Directivo])));
        Assert.Equal([CodigoRol.Administrador], _admin.Roles);
    }

    [Fact]
    public async Task Un_administrador_no_puede_quitarse_su_rol_aunque_haya_otros()
    {
        var actor = await PrepararAdminAsync();
        await _e.AgregarUsuarioAsync("admin2", "Clave2026", CodigoRol.Administrador);

        await Assert.ThrowsAsync<ReglaNegocioError>(() => new CambiarRolesCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj)
            .EjecutarAsync(new(actor, actor, [CodigoRol.Directivo])));
    }

    // ---------- Desactivar y reactivar ----------

    [Fact]
    public async Task Desactivar_impide_iniciar_sesion_y_registra_el_evento()
    {
        var actor = await PrepararAdminAsync();
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);

        await new DesactivarUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj).EjecutarAsync(new(actor, medico.Id));

        Assert.Equal(EstadoUsuario.Desactivado, medico.Estado);
        Assert.Equal(TipoEventoSeguridad.UsuarioDesactivado, UltimoEvento().Tipo);
        Assert.False(medico.IntentarAutenticar("Clave2026", _e.Hasher, _e.Reloj).Exitoso);
    }

    [Fact]
    public async Task Un_administrador_no_puede_desactivarse_a_si_mismo()
    {
        var actor = await PrepararAdminAsync();
        await _e.AgregarUsuarioAsync("admin2", "Clave2026", CodigoRol.Administrador);

        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            new DesactivarUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj).EjecutarAsync(new(actor, actor)));
    }

    [Fact]
    public async Task No_se_puede_desactivar_al_ultimo_administrador_activo()
    {
        await PrepararAdminAsync();

        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            new DesactivarUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj).EjecutarAsync(new(Guid.NewGuid(), _admin.Id)));
    }

    [Fact]
    public async Task Reactivar_vuelve_a_permitir_el_inicio_de_sesion()
    {
        var actor = await PrepararAdminAsync();
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);
        medico.Desactivar();

        await new ReactivarUsuarioCasoUso(_e.Usuarios, _e.Eventos, _e.Reloj).EjecutarAsync(new(actor, medico.Id));

        Assert.Equal(EstadoUsuario.Activo, medico.Estado);
        Assert.Equal(TipoEventoSeguridad.UsuarioReactivado, UltimoEvento().Tipo);
    }

    // ---------- Restablecer contraseña ----------

    [Fact]
    public async Task Restablecer_asigna_la_temporal_exige_cambiarla_y_no_la_registra()
    {
        var actor = await PrepararAdminAsync();
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);

        await new RestablecerContrasenaCasoUso(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Reloj)
            .EjecutarAsync(new(actor, medico.Id, "Temporal2026"));

        Assert.True(medico.CambioContrasenaPendiente);
        Assert.True(medico.IntentarAutenticar("Temporal2026", _e.Hasher, _e.Reloj).Exitoso);
        Assert.Equal(TipoEventoSeguridad.ContrasenaRestablecida, UltimoEvento().Tipo);
        Assert.DoesNotContain("Temporal2026", UltimoEvento().Detalle);
    }

    [Fact]
    public async Task Restablecer_con_una_temporal_que_no_cumple_la_politica_se_rechaza()
    {
        var actor = await PrepararAdminAsync();
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);

        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            new RestablecerContrasenaCasoUso(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Reloj)
                .EjecutarAsync(new(actor, medico.Id, "sololetras")));
    }

    // ---------- Listar y consultar ----------

    [Fact]
    public async Task Listar_busca_por_nombre_o_usuario_y_filtra_por_rol_y_estado()
    {
        await PrepararAdminAsync();
        await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);
        var med2 = await _e.AgregarUsuarioAsync("med02", "Clave2026", CodigoRol.Medico, CodigoRol.Epidemiologo);
        await _e.AgregarUsuarioAsync("farm01", "Clave2026", CodigoRol.Farmaceutico);
        med2.Desactivar();
        var casoUso = new ListarUsuariosCasoUso(_e.Usuarios, _e.Reloj);

        var porBusqueda = await casoUso.EjecutarAsync(new(Busqueda: "MED"));
        var porRol = await casoUso.EjecutarAsync(new(Rol: CodigoRol.Medico, Estado: EstadoUsuario.Activo));

        Assert.Equal(2, porBusqueda.Total);
        var unico = Assert.Single(porRol.Elementos);
        Assert.Equal("med01", unico.NombreUsuario);
    }

    [Fact]
    public async Task Listar_pagina_los_resultados()
    {
        await PrepararAdminAsync();
        for (var i = 0; i < 5; i++) await _e.AgregarUsuarioAsync($"lab0{i}", "Clave2026", CodigoRol.PersonalLaboratorio);

        var pagina = await new ListarUsuariosCasoUso(_e.Usuarios, _e.Reloj).EjecutarAsync(new(Pagina: 2, Tamano: 4));

        Assert.Equal(6, pagina.Total);
        Assert.Equal(2, pagina.Elementos.Count);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_rechaza_paginacion_invalida(int numeroPagina, int tamano)
    {
        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            new ListarUsuariosCasoUso(_e.Usuarios, _e.Reloj).EjecutarAsync(new(Pagina: numeroPagina, Tamano: tamano)));
    }

    [Fact]
    public async Task Consultar_indica_si_el_usuario_esta_bloqueado()
    {
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);
        for (var i = 0; i < 5; i++) medico.IntentarAutenticar("Incorrecta1", _e.Hasher, _e.Reloj);

        var dto = await new ConsultarUsuarioCasoUso(_e.Usuarios, _e.Reloj).EjecutarAsync(medico.Id);

        Assert.True(dto.Bloqueado);
    }
}
