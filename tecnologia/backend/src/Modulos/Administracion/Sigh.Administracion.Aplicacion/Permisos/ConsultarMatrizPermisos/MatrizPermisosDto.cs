using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Aplicacion.Permisos.ConsultarMatrizPermisos;

/// <summary>Matriz fija de roles y permisos (contrato MatrizPermisos).</summary>
public sealed record MatrizPermisosDto(IReadOnlyList<RolDto> Roles, IReadOnlyList<PermisoDto> Permisos);

public sealed record RolDto(CodigoRol Codigo, string Nombre);

public sealed record PermisoDto(string Codigo, string Descripcion, IReadOnlyList<CodigoRol> Roles);
