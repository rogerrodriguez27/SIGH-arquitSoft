namespace Sigh.Administracion.Aplicacion.Seguridad.RegistrarAccesoDenegado;

/// <param name="Motivo">Por ejemplo, el permiso que faltó; nunca datos clínicos ni secretos.</param>
public sealed record RegistrarAccesoDenegadoComando(Guid? ActorId, string Metodo, string Ruta, string Motivo);
