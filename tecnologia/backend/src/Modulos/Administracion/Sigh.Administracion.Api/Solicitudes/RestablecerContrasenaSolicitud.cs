using System.ComponentModel.DataAnnotations;

namespace Sigh.Administracion.Api.Solicitudes;

/// <summary>Cuerpo de POST /api/v1/usuarios/{id}/restablecer-contrasena.</summary>
public sealed record RestablecerContrasenaSolicitud(
    [Required(ErrorMessage = "La contraseña temporal es obligatoria.")]
    [StringLength(128, MinimumLength = 8)]
    string ContrasenaTemporal)
{
    public override string ToString() => "RestablecerContrasenaSolicitud { ******** }";
}
