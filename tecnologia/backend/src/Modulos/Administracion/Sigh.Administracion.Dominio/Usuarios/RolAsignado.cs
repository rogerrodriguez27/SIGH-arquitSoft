using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Dominio.Usuarios;

/// <summary>Un rol asignado a un usuario (fila de usuario_roles). Solo lo crea <see cref="Usuario"/>.</summary>
public sealed class RolAsignado
{
    public Guid UsuarioId { get; private set; }
    public CodigoRol Rol { get; private set; }

    private RolAsignado() { }                                        // requerido por EF Core

    internal RolAsignado(Guid usuarioId, CodigoRol rol)
    {
        UsuarioId = usuarioId;
        Rol = rol;
    }
}
