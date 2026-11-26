using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Aplicacion.Permisos.ConsultarMatrizPermisos;

/// <summary>
/// Entrega la matriz fija para que el administrador la consulte (FR-013). No hay casos de uso para
/// crear roles ni cambiar permisos: solo cambian con una nueva versión del sistema.
/// </summary>
public sealed class ConsultarMatrizPermisosCasoUso
{
    public MatrizPermisosDto Ejecutar() => new(
        CatalogoRoles.Todos.Select(r => new RolDto(r.Codigo, r.Nombre)).ToList(),
        MatrizPermisos.Filas().Select(f => new PermisoDto(f.Codigo, f.Descripcion, f.Roles)).ToList());
}
