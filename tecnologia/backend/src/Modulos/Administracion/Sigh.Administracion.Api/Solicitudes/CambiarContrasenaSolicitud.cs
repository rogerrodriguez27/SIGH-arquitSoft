using System.ComponentModel.DataAnnotations;

namespace Sigh.Administracion.Api.Solicitudes;

/// <summary>Cuerpo de POST /api/v1/autenticacion/cambiar-contrasena (contrato CambiarContrasenaSolicitud).</summary>
public sealed record CambiarContrasenaSolicitud(
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    string ContrasenaActual,
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [MinLength(8)]
    [MaxLength(128)]
    string ContrasenaNueva)
{
    public override string ToString() => "CambiarContrasenaSolicitud { ******** }";
}
