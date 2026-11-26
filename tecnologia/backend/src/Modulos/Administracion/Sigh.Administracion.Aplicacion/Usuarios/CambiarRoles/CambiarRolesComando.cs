using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Aplicacion.Usuarios.CambiarRoles;

public sealed record CambiarRolesComando(Guid ActorId, Guid UsuarioId, IReadOnlyList<CodigoRol> Roles);
