using Sigh.Compartido.Persistencia;

namespace Sigh.Administracion.Aplicacion.Tests.Dobles;

/// <summary>Unidad de trabajo en memoria: solo indica si hay una transacción activa.</summary>
public sealed class UnidadDeTrabajoFalsa : IUnidadDeTrabajo
{
    public bool TransaccionActiva { get; private set; }

    public async Task EjecutarEnTransaccionAsync(Func<Task> operacion, CancellationToken ct = default)
    {
        TransaccionActiva = true;
        try
        {
            await operacion();
        }
        finally
        {
            TransaccionActiva = false;
        }
    }
}
