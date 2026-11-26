using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Dominio.Tests.Permisos;

/// <summary>La matriz del código debe ser idéntica a la tabla "Matriz inicial de permisos" de spec.md.</summary>
public sealed class MatrizPermisosTests
{
    private const CodigoRol Adm = CodigoRol.Administrador;
    private const CodigoRol Admi = CodigoRol.PersonalAdmision;
    private const CodigoRol Tri = CodigoRol.EnfermeroTriaje;
    private const CodigoRol Med = CodigoRol.Medico;
    private const CodigoRol Far = CodigoRol.Farmaceutico;
    private const CodigoRol Lab = CodigoRol.PersonalLaboratorio;
    private const CodigoRol Epi = CodigoRol.Epidemiologo;
    private const CodigoRol Dir = CodigoRol.Directivo;

    public static TheoryData<string, CodigoRol[]> TablaDeLaSpec => new()
    {
        { CodigoPermiso.GestionarUsuarios, [Adm] },
        { CodigoPermiso.ConsultarBitacora, [Adm] },
        { CodigoPermiso.RegistrarPacientes, [Admi, Tri] },
        { CodigoPermiso.RegistrarTriaje, [Tri] },
        { CodigoPermiso.ConsultarHce, [Med] },
        { CodigoPermiso.RegistrarAtencion, [Med] },
        { CodigoPermiso.RecetasExamenes, [Med] },
        { CodigoPermiso.DispensarRecetas, [Far] },
        { CodigoPermiso.GestionarInventario, [Far] },
        { CodigoPermiso.ResultadosLaboratorio, [Lab] },
        { CodigoPermiso.ReportesEpidemiologicos, [Epi] },
        { CodigoPermiso.DashboardsGestion, [Epi, Dir] },
        { CodigoPermiso.RecibirNotificaciones, [Adm, Admi, Tri, Med, Far, Lab, Epi, Dir] },
    };

    [Theory]
    [MemberData(nameof(TablaDeLaSpec))]
    public void Cada_fila_coincide_con_la_spec(string permiso, CodigoRol[] rolesEsperados)
    {
        var fila = MatrizPermisos.Filas().Single(f => f.Codigo == permiso);

        Assert.Equal(rolesEsperados.OrderBy(r => r), fila.Roles.OrderBy(r => r));
    }

    [Fact]
    public void Tiene_13_permisos_y_8_roles()
    {
        Assert.Equal(13, MatrizPermisos.Filas().Count);
        Assert.Equal(8, Enum.GetValues<CodigoRol>().Length);
    }

    [Fact]
    public void PermisosDe_devuelve_la_union_de_los_roles()
    {
        var permisos = MatrizPermisos.PermisosDe([Med, Epi]);

        Assert.Contains(CodigoPermiso.ConsultarHce, permisos);
        Assert.Contains(CodigoPermiso.ReportesEpidemiologicos, permisos);
        Assert.Contains(CodigoPermiso.DashboardsGestion, permisos);
        Assert.DoesNotContain(CodigoPermiso.GestionarUsuarios, permisos);
    }

    [Fact]
    public void El_administrador_no_tiene_acceso_al_contenido_clinico()
    {
        var permisos = MatrizPermisos.PermisosDe([Adm]);

        Assert.DoesNotContain(CodigoPermiso.ConsultarHce, permisos);
        Assert.DoesNotContain(CodigoPermiso.RegistrarAtencion, permisos);
    }
}
