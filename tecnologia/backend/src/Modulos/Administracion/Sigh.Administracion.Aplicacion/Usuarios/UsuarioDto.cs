using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Aplicacion.Usuarios;

/// <summary>Datos de un usuario para la API (contrato Usuario). Nunca incluye el hash de la contraseña.</summary>
public sealed record UsuarioDto(
    Guid Id,
    string NombreUsuario,
    string NombreCompleto,
    EstadoUsuario Estado,
    IReadOnlyList<CodigoRol> Roles,
    bool CambioContrasenaPendiente,
    bool Bloqueado,
    DateTimeOffset CreadoEn,
    DateTimeOffset? UltimoAcceso)
{
    internal static UsuarioDto Desde(Usuario usuario, DateTimeOffset ahora) => new(
        usuario.Id,
        usuario.NombreUsuario,
        usuario.NombreCompleto,
        usuario.Estado,
        usuario.Roles.Order().ToList(),
        usuario.CambioContrasenaPendiente,
        usuario.EstaBloqueado(ahora),
        usuario.CreadoEn,
        usuario.UltimoAcceso);
}
