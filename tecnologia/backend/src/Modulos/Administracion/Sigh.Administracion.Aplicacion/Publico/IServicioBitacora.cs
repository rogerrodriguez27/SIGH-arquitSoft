namespace Sigh.Administracion.Aplicacion.Publico;

/// <summary>
/// Única puerta por la que los demás módulos (HCE, Admisión y Triaje, Farmacia y Laboratorio) registran
/// accesos a historias clínicas (FR-018, contracts/servicio-bitacora.md). Es lo único que esos módulos
/// pueden referenciar del módulo Administración (principio II).
/// </summary>
public interface IServicioBitacora
{
    /// Registra un acceso PERMITIDO dentro de la transacción activa de la unidad de trabajo.
    /// Si no hay una transacción activa, lanza InvalidOperationException: obliga a cumplir FR-022.
    Task RegistrarAccesoAsync(RegistroAcceso registro, CancellationToken ct = default);

    /// Registra un intento DENEGADO en una transacción propia e independiente,
    /// para que quede guardado aunque la operación original se revierta (FR-020).
    Task RegistrarIntentoDenegadoAsync(RegistroAcceso registro, CancellationToken ct = default);
}
