using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Dominio.Permisos;

/// <summary>
/// Matriz fija de permisos por rol: traducción literal de la tabla "Matriz inicial de permisos" de la
/// spec (FR-012, FR-013). No se guarda en la base de datos para decidir permisos.
/// </summary>
public static class MatrizPermisos
{
    private static readonly IReadOnlyDictionary<string, CodigoRol[]> RolesPorPermiso =
        new Dictionary<string, CodigoRol[]>
        {
            [CodigoPermiso.GestionarUsuarios] = [CodigoRol.Administrador],
            [CodigoPermiso.ConsultarBitacora] = [CodigoRol.Administrador],
            [CodigoPermiso.RegistrarPacientes] = [CodigoRol.PersonalAdmision, CodigoRol.EnfermeroTriaje],
            [CodigoPermiso.RegistrarTriaje] = [CodigoRol.EnfermeroTriaje],
            [CodigoPermiso.ConsultarHce] = [CodigoRol.Medico],
            [CodigoPermiso.RegistrarAtencion] = [CodigoRol.Medico],
            [CodigoPermiso.RecetasExamenes] = [CodigoRol.Medico],
            [CodigoPermiso.DispensarRecetas] = [CodigoRol.Farmaceutico],
            [CodigoPermiso.GestionarInventario] = [CodigoRol.Farmaceutico],
            [CodigoPermiso.ResultadosLaboratorio] = [CodigoRol.PersonalLaboratorio],
            [CodigoPermiso.ReportesEpidemiologicos] = [CodigoRol.Epidemiologo],
            [CodigoPermiso.DashboardsGestion] = [CodigoRol.Epidemiologo, CodigoRol.Directivo],
            [CodigoPermiso.RecibirNotificaciones] = Enum.GetValues<CodigoRol>(),
        };

    /// <summary>Unión de los permisos de todos los roles indicados (historia 3, escenario 3).</summary>
    public static IReadOnlySet<string> PermisosDe(IEnumerable<CodigoRol> roles)
    {
        var conjunto = roles.ToHashSet();
        return CodigoPermiso.Todos
            .Where(p => RolesPorPermiso[p.Codigo].Any(conjunto.Contains))
            .Select(p => p.Codigo)
            .ToHashSet();
    }

    /// <summary>Filas de la matriz para mostrarla: permiso, descripción y roles que lo tienen.</summary>
    public static IReadOnlyList<(string Codigo, string Descripcion, IReadOnlyList<CodigoRol> Roles)> Filas() =>
        CodigoPermiso.Todos
            .Select(p => (p.Codigo, p.Descripcion, (IReadOnlyList<CodigoRol>)RolesPorPermiso[p.Codigo]))
            .ToList();
}
