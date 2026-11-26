namespace Sigh.Administracion.Dominio.Usuarios;

/// <summary>Estado de la cuenta. No existe el estado "eliminado" (FR-009).</summary>
public enum EstadoUsuario
{
    Activo,
    Desactivado,
}
