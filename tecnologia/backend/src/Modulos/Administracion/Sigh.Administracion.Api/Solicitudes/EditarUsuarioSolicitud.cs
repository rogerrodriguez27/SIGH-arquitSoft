using System.ComponentModel.DataAnnotations;

namespace Sigh.Administracion.Api.Solicitudes;

/// <summary>Cuerpo de PUT /api/v1/usuarios/{id} (contrato EditarUsuarioSolicitud).</summary>
public sealed record EditarUsuarioSolicitud(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, MinimumLength = 3)]
    string NombreCompleto);
