using System.ComponentModel.DataAnnotations;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Api.Solicitudes;

/// <summary>Cuerpo de PUT /api/v1/usuarios/{id}/roles.</summary>
public sealed record CambiarRolesSolicitud(
    [Required(ErrorMessage = "Debe indicar al menos un rol.")]
    [MinLength(1, ErrorMessage = "Debe indicar al menos un rol.")]
    List<CodigoRol> Roles);
