namespace Sigh.Compartido.Persistencia;

/// <summary>Unidad de trabajo sobre la conexión compartida de la petición (research D3).</summary>
public sealed class UnidadDeTrabajoNpgsql(ConexionOltp conexion) : IUnidadDeTrabajo
{
    public bool TransaccionActiva => conexion.Transaccion is not null;

    public async Task EjecutarEnTransaccionAsync(Func<Task> operacion, CancellationToken ct = default)
    {
        if (TransaccionActiva)
        {
            await operacion();
            return;
        }

        await conexion.AbrirAsync(ct);
        await using var transaccion = await conexion.Conexion.BeginTransactionAsync(ct);
        conexion.Transaccion = transaccion;
        try
        {
            await operacion();
            await transaccion.CommitAsync(ct);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            conexion.Transaccion = null;
        }
    }
}
