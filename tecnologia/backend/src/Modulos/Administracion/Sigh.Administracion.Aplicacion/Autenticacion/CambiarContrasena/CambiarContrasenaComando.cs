namespace Sigh.Administracion.Aplicacion.Autenticacion.CambiarContrasena;

public sealed record CambiarContrasenaComando(Guid UsuarioId, string ContrasenaActual, string ContrasenaNueva)
{
    public override string ToString() => $"CambiarContrasenaComando {{ UsuarioId = {UsuarioId} }}";
}
