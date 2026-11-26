using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Tests.Dobles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Dominio.Tests.Usuarios;

/// <summary>Gestión de usuarios por el administrador (FR-008, FR-010, FR-015).</summary>
public sealed class UsuarioGestionTests
{
    private readonly HasherFalso _hasher = new();
    private readonly RelojFijo _reloj = RelojFijo.Nuevo();

    private Usuario CrearUsuario(params CodigoRol[] roles) =>
        Usuario.Crear("farm01", "Usuario Ficticio", ContrasenaPropuesta.Crear("Clave2026"),
            roles.Length == 0 ? [CodigoRol.Farmaceutico] : roles, _hasher, _reloj);

    [Fact]
    public void AsignarRoles_reemplaza_los_roles()
    {
        var usuario = CrearUsuario(CodigoRol.Medico);

        usuario.AsignarRoles([CodigoRol.Medico, CodigoRol.Epidemiologo]);

        Assert.Equal([CodigoRol.Medico, CodigoRol.Epidemiologo], usuario.Roles.Order());
    }

    [Fact]
    public void AsignarRoles_exige_al_menos_un_rol()
    {
        var usuario = CrearUsuario(CodigoRol.Medico);

        Assert.Throws<ReglaNegocioError>(() => usuario.AsignarRoles([]));
        Assert.Equal([CodigoRol.Medico], usuario.Roles);
    }

    [Fact]
    public void EditarDatos_cambia_el_nombre_completo_validado()
    {
        var usuario = CrearUsuario();

        usuario.EditarDatos("  Nombre Corregido ");

        Assert.Equal("Nombre Corregido", usuario.NombreCompleto);
        Assert.Throws<ReglaNegocioError>(() => usuario.EditarDatos("ab"));
    }

    [Fact]
    public void Desactivar_y_Reactivar_cambian_el_estado()
    {
        var usuario = CrearUsuario();

        usuario.Desactivar();
        Assert.Equal(EstadoUsuario.Desactivado, usuario.Estado);

        usuario.Reactivar();
        Assert.Equal(EstadoUsuario.Activo, usuario.Estado);
    }

    [Fact]
    public void Un_usuario_desactivado_no_puede_iniciar_sesion()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();

        var resultado = usuario.IntentarAutenticar("Clave2026", _hasher, _reloj);

        Assert.False(resultado.Exitoso);
        Assert.Equal(MotivoRechazo.Desactivado, resultado.Motivo);
    }

    [Fact]
    public void RestablecerContrasena_asigna_la_temporal_exige_cambiarla_y_quita_el_bloqueo()
    {
        var usuario = CrearUsuario();
        usuario.CambiarContrasena("Clave2026", ContrasenaPropuesta.Crear("Propia2026"), _hasher);
        for (var i = 0; i < 5; i++) usuario.IntentarAutenticar("Incorrecta1", _hasher, _reloj);

        usuario.RestablecerContrasena(ContrasenaPropuesta.Crear("Temporal2026"), _hasher);

        Assert.True(usuario.CambioContrasenaPendiente);
        Assert.Null(usuario.BloqueadoHasta);
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.True(usuario.IntentarAutenticar("Temporal2026", _hasher, _reloj).Exitoso);
    }
}
