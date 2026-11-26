using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Aplicacion.Usuarios.CrearUsuario;

public sealed record CrearUsuarioComando(
    Guid ActorId,
    string NombreUsuario,
    string NombreCompleto,
    string ContrasenaInicial,
    IReadOnlyList<CodigoRol> Roles)
{
    public override string ToString() => $"CrearUsuarioComando {{ NombreUsuario = {NombreUsuario} }}";
}
