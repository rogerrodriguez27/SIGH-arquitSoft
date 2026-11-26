namespace Sigh.Administracion.Aplicacion.Usuarios.RestablecerContrasena;

public sealed record RestablecerContrasenaComando(Guid ActorId, Guid UsuarioId, string ContrasenaTemporal)
{
    public override string ToString() => $"RestablecerContrasenaComando {{ UsuarioId = {UsuarioId} }}";
}
