using Npgsql;

namespace Sigh.Compartido.Persistencia;

/// <summary>
/// Conexión a sigh_oltp compartida por todos los módulos durante una petición (ámbito scoped, research D3).
/// Cada DbContext de cada módulo usa esta conexión y se une a la transacción activa.
/// </summary>
public sealed class ConexionOltp(NpgsqlDataSource fuenteDatos) : IAsyncDisposable
{
    public NpgsqlConnection Conexion { get; } = fuenteDatos.CreateConnection();

    /// <summary>Transacción abierta por <see cref="IUnidadDeTrabajo"/>, o null si no hay ninguna.</summary>
    public NpgsqlTransaction? Transaccion { get; internal set; }

    internal async Task AbrirAsync(CancellationToken ct)
    {
        if (Conexion.State != System.Data.ConnectionState.Open)
            await Conexion.OpenAsync(ct);
    }

    public ValueTask DisposeAsync() => Conexion.DisposeAsync();
}
