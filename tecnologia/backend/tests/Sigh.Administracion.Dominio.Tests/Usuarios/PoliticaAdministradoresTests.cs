using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Tests.Dobles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Dominio.Tests.Usuarios;

/// <summary>Reglas que involucran a varios usuarios (FR-014, data-model.md PoliticaAdministradores).</summary>
public sealed class PoliticaAdministradoresTests
{
    private static Usuario Nuevo(params CodigoRol[] roles) =>
        Usuario.Crear("u" + Guid.NewGuid().ToString("N")[..8], "Usuario Ficticio",
            ContrasenaPropuesta.Crear("Clave2026"), roles, new HasherFalso(), RelojFijo.Nuevo());

    [Fact]
    public void No_se_puede_desactivar_al_ultimo_administrador_activo()
    {
        var actor = Guid.NewGuid();
        var ultimo = Nuevo(CodigoRol.Administrador);

        Assert.Throws<ReglaNegocioError>(() =>
            PoliticaAdministradores.ValidarDesactivacion(actor, ultimo, administradoresActivos: 1));
    }

    [Fact]
    public void Se_puede_desactivar_a_un_administrador_si_quedan_otros()
    {
        var otro = Nuevo(CodigoRol.Administrador);

        PoliticaAdministradores.ValidarDesactivacion(Guid.NewGuid(), otro, administradoresActivos: 2);
    }

    [Fact]
    public void Un_administrador_no_puede_desactivarse_a_si_mismo()
    {
        var yo = Nuevo(CodigoRol.Administrador);

        Assert.Throws<ReglaNegocioError>(() =>
            PoliticaAdministradores.ValidarDesactivacion(yo.Id, yo, administradoresActivos: 3));
    }

    [Fact]
    public void Se_puede_desactivar_a_un_usuario_que_no_es_administrador()
    {
        var medico = Nuevo(CodigoRol.Medico);

        PoliticaAdministradores.ValidarDesactivacion(Guid.NewGuid(), medico, administradoresActivos: 1);
    }

    [Fact]
    public void No_se_puede_quitar_el_rol_Administrador_al_ultimo_administrador_activo()
    {
        var ultimo = Nuevo(CodigoRol.Administrador);

        Assert.Throws<ReglaNegocioError>(() => PoliticaAdministradores.ValidarCambioRoles(
            Guid.NewGuid(), ultimo, [CodigoRol.Directivo], administradoresActivos: 1));
    }

    [Fact]
    public void Un_administrador_no_puede_quitarse_su_propio_rol_Administrador()
    {
        var yo = Nuevo(CodigoRol.Administrador);

        Assert.Throws<ReglaNegocioError>(() => PoliticaAdministradores.ValidarCambioRoles(
            yo.Id, yo, [CodigoRol.Directivo], administradoresActivos: 3));
    }

    [Fact]
    public void Un_administrador_puede_agregarse_otros_roles_a_si_mismo()
    {
        var yo = Nuevo(CodigoRol.Administrador);

        PoliticaAdministradores.ValidarCambioRoles(
            yo.Id, yo, [CodigoRol.Administrador, CodigoRol.Directivo], administradoresActivos: 1);
    }

    [Fact]
    public void Quitar_el_rol_a_un_administrador_desactivado_no_afecta_a_los_activos()
    {
        var inactivo = Nuevo(CodigoRol.Administrador);
        inactivo.Desactivar();

        PoliticaAdministradores.ValidarCambioRoles(
            Guid.NewGuid(), inactivo, [CodigoRol.Directivo], administradoresActivos: 1);
    }
}
