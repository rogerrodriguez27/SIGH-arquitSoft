using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Aplicacion.Autenticacion;

/// <summary>Sesión entregada al usuario: token firmado y datos para construir el menú (contrato SesionRespuesta).</summary>
public sealed record SesionResultado(
    string Token,
    DateTimeOffset ExpiraEn,
    Guid UsuarioId,
    string NombreUsuario,
    string NombreCompleto,
    IReadOnlyList<CodigoRol> Roles,
    IReadOnlyList<string> Permisos,
    bool CambioContrasenaPendiente)
{
    /// <summary>Calcula los permisos efectivos con la matriz fija y emite el token (research D4).</summary>
    internal static SesionResultado Emitir(Usuario usuario, IEmisorTokens emisor)
    {
        var permisos = MatrizPermisos.PermisosDe(usuario.Roles).ToList();
        var token = emisor.Emitir(usuario, permisos);
        return new SesionResultado(token.Token, token.ExpiraEn, usuario.Id, usuario.NombreUsuario,
            usuario.NombreCompleto, usuario.Roles, permisos, usuario.CambioContrasenaPendiente);
    }
}
