using Microsoft.EntityFrameworkCore;
using Sigh.Administracion.Infraestructura.Persistencia;

namespace Sigh.Api.Arranque;

/// <summary>Aplica las migraciones pendientes de cada módulo al arrancar.</summary>
public static class AplicarMigraciones
{
    public static async Task AplicarMigracionesAsync(this WebApplication app)
    {
        await using var ambito = app.Services.CreateAsyncScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<AdministracionDbContext>();
        await contexto.Database.MigrateAsync();
    }
}
