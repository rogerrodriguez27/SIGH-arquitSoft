namespace Sigh.Administracion.Dominio.Usuarios;

/// <summary>
/// Resultado de un intento de inicio de sesión. El motivo es interno: la API siempre responde con un
/// mensaje genérico (FR-003, research D6).
/// </summary>
public sealed record ResultadoAutenticacion(bool Exitoso, MotivoRechazo? Motivo, bool CuentaBloqueadaAhora)
{
    public static ResultadoAutenticacion Correcto() => new(true, null, false);

    public static ResultadoAutenticacion Rechazado(MotivoRechazo motivo, bool cuentaBloqueadaAhora = false) =>
        new(false, motivo, cuentaBloqueadaAhora);
}

public enum MotivoRechazo
{
    UsuarioInexistente,
    Desactivado,
    Bloqueado,
    ContrasenaIncorrecta,
}
