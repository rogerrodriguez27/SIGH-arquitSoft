using System.ComponentModel.DataAnnotations;

namespace Sigh.Administracion.Api.Solicitudes;

/// <summary>Cuerpo de POST /api/v1/autenticacion/iniciar-sesion (contrato IniciarSesionSolicitud).</summary>
public sealed record IniciarSesionSolicitud(
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [MaxLength(50)]
    string NombreUsuario,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(128)]
    string Contrasena)
{
    public override string ToString() => $"IniciarSesionSolicitud {{ NombreUsuario = {NombreUsuario} }}";
}
