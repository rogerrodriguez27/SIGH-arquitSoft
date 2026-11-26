using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Sigh.Api.Salud;

/// <summary>Healthcheck de /salud: comprueba que se puede abrir una conexión a sigh_oltp.</summary>
public sealed class VerificacionBaseDatos(NpgsqlDataSource fuenteDatos) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conexion = await fuenteDatos.OpenConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar con la base de datos.");
        }
    }
}
