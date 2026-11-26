namespace Sigh.Administracion.Dominio.Seguridad;

/// <summary>Tipos de evento de seguridad (data-model.md, EventoSeguridad.Tipo).</summary>
public enum TipoEventoSeguridad
{
    InicioSesionExitoso,
    InicioSesionFallido,
    CuentaBloqueada,
    ContrasenaCambiada,
    ContrasenaRestablecida,
    UsuarioCreado,
    UsuarioEditado,
    RolesCambiados,
    UsuarioDesactivado,
    UsuarioReactivado,
    BitacoraConsultada,
    AccesoDenegado,
}

public static class TipoEventoSeguridadExtensiones
{
    /// <summary>Código que se guarda en la base de datos; por ejemplo, INICIO_SESION_EXITOSO.</summary>
    public static string ACodigo(this TipoEventoSeguridad tipo) =>
        string.Concat(tipo.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()))
            .ToUpperInvariant();

    public static TipoEventoSeguridad DesdeCodigo(string codigo) =>
        Enum.GetValues<TipoEventoSeguridad>().Single(t => t.ACodigo() == codigo);
}
