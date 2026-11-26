using Sigh.Administracion.Aplicacion.Autenticacion;

namespace Sigh.Administracion.Api.Respuestas;

/// <summary>Respuesta de inicio de sesión y de cambio de contraseña (contrato SesionRespuesta).</summary>
public sealed record SesionRespuesta(string Token, DateTimeOffset ExpiraEn, UsuarioSesionRespuesta Usuario)
{
    public static SesionRespuesta Desde(SesionResultado sesion) => new(
        sesion.Token,
        sesion.ExpiraEn,
        new UsuarioSesionRespuesta(sesion.UsuarioId, sesion.NombreUsuario, sesion.NombreCompleto,
            sesion.Roles.Select(r => r.ToString()).ToList(), sesion.Permisos, sesion.CambioContrasenaPendiente));
}

public sealed record UsuarioSesionRespuesta(
    Guid Id,
    string NombreUsuario,
    string NombreCompleto,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permisos,
    bool CambioContrasenaPendiente);
