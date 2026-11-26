namespace Sigh.Administracion.Aplicacion.Publico;

/// <summary>
/// Datos de un acceso a una historia clínica (contracts/servicio-bitacora.md). No tiene campos para
/// contenido clínico (FR-023). La fecha y hora y el nombre de usuario los completa Administración.
/// </summary>
public sealed record RegistroAcceso(
    Guid UsuarioId,
    string Rol,              // CodigoRol con el que actuó
    string Accion,           // p. ej. "REGISTRAR_ATENCION" (lista cerrada en data-model.md)
    Guid HistoriaId,
    string ModuloOrigen);    // "Hce", "Admision", "Farmacia", "Laboratorio"
