namespace Sigh.Administracion.Aplicacion.Usuarios.EditarUsuario;

public sealed record EditarUsuarioComando(Guid ActorId, Guid UsuarioId, string NombreCompleto);
