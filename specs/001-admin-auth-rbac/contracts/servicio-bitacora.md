# Contrato público: `IServicioBitacora` (FR-018)

Es la única puerta por la que los demás módulos (HCE, Admisión y Triaje, Farmacia y Laboratorio)
registran accesos a historias clínicas. Vive en
`Sigh.Administracion.Aplicacion/Publico/` y es lo único que esos módulos pueden referenciar del
módulo Administración (principio II).

```csharp
namespace Sigh.Administracion.Aplicacion.Publico;

public interface IServicioBitacora
{
    /// Registra un acceso PERMITIDO dentro de la transacción activa de la unidad de trabajo.
    /// Si no hay una transacción activa, lanza InvalidOperationException: obliga a cumplir FR-022.
    Task RegistrarAccesoAsync(RegistroAcceso registro, CancellationToken ct = default);

    /// Registra un intento DENEGADO en una transacción propia e independiente,
    /// para que quede guardado aunque la operación original se revierta (FR-020).
    Task RegistrarIntentoDenegadoAsync(RegistroAcceso registro, CancellationToken ct = default);
}

public sealed record RegistroAcceso(
    Guid UsuarioId,
    string Rol,              // CodigoRol con el que actuó
    string Accion,           // p. ej. "REGISTRAR_ATENCION" (lista cerrada en data-model.md)
    Guid HistoriaId,
    string ModuloOrigen);    // "Hce", "Admision", "Farmacia", "Laboratorio"
```

## Reglas

- La fecha y hora las pone siempre el servidor (`IReloj`); el módulo que llama no las envía.
- El nombre de usuario se completa dentro de Administración; quien llama no lo envía.
- `RegistroAcceso` no tiene campos para contenido clínico (FR-023).
- Si `Accion` o `ModuloOrigen` no están en la lista cerrada, se lanza `ArgumentException`.
- Ejemplo de uso en la HCE (funcionalidad futura):

```csharp
await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async () =>
{
    await _repositorioHistorias.GuardarAtencionAsync(atencion);
    await _bitacora.RegistrarAccesoAsync(new RegistroAcceso(medicoId, "Medico",
        "REGISTRAR_ATENCION", historia.Id, "Hce"));
}); // ambas escrituras se confirman juntas o ninguna (FR-022)
```

## Verificación en esta funcionalidad

En `Sigh.Administracion.Integracion.Tests` hay un "módulo simulado" con su propia tabla en otro
esquema. La prueba comprueba lo siguiente:

1. Si la escritura del módulo y la de la bitácora se confirman, ambas quedan guardadas.
2. Si la bitácora falla, la escritura del módulo se revierte.
3. Un intento denegado queda guardado aunque la operación se revierta.
