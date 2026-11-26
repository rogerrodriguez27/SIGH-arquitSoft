namespace Sigh.Administracion.Aplicacion.Autenticacion.IniciarSesion;

public sealed record IniciarSesionComando(string NombreUsuario, string Contrasena)
{
    public override string ToString() => $"IniciarSesionComando {{ NombreUsuario = {NombreUsuario} }}";
}
