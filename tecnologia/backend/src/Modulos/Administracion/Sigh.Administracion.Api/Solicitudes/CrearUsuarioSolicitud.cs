using System.ComponentModel.DataAnnotations;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Api.Solicitudes;

/// <summary>
/// Cuerpo de POST /api/v1/usuarios (contrato CrearUsuarioSolicitud). Los caracteres permitidos del
/// nombre de usuario los valida el dominio después de normalizarlo (quitar espacios, minúsculas).
/// </summary>
public sealed record CrearUsuarioSolicitud(
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3)]
    string NombreUsuario,
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, MinimumLength = 3)]
    string NombreCompleto,
    [Required(ErrorMessage = "La contraseña inicial es obligatoria.")]
    [StringLength(128, MinimumLength = 8)]
    string ContrasenaInicial,
    [Required(ErrorMessage = "Debe indicar al menos un rol.")]
    [MinLength(1, ErrorMessage = "Debe indicar al menos un rol.")]
    List<CodigoRol> Roles)
{
    public override string ToString() => $"CrearUsuarioSolicitud {{ NombreUsuario = {NombreUsuario} }}";
}
