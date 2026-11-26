namespace Sigh.Compartido.Persistencia;

/// <summary>
/// Ejecuta varias escrituras, incluso de módulos distintos, en una sola transacción de PostgreSQL
/// (FR-022, research D3).
/// </summary>
public interface IUnidadDeTrabajo
{
    bool TransaccionActiva { get; }

    /// <summary>
    /// Abre una transacción, ejecuta la operación y confirma. Si la operación falla, revierte.
    /// Si ya hay una transacción activa, la operación se une a ella.
    /// </summary>
    Task EjecutarEnTransaccionAsync(Func<Task> operacion, CancellationToken ct = default);
}
