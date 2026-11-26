using Sigh.Administracion.Aplicacion.AdministradorInicial;

namespace Sigh.Api.Arranque;

/// <summary>
/// Después de las migraciones, crea el administrador inicial a partir de AdminInicial__* (FR-016,
/// research D7). Los mensajes de log nunca muestran los valores.
/// </summary>
public static class CrearAdministradorInicial
{
    public static async Task CrearAdministradorInicialAsync(this WebApplication app)
    {
        await using var ambito = app.Services.CreateAsyncScope();
        var casoUso = ambito.ServiceProvider.GetRequiredService<CrearAdministradorInicialCasoUso>();
        var logger = ambito.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdministradorInicial");

        var seccion = app.Configuration.GetSection("AdminInicial");
        var resultado = await casoUso.EjecutarAsync(new DatosAdministradorInicial(
            seccion["Usuario"], seccion["NombreCompleto"], seccion["Contrasena"]));

        switch (resultado)
        {
            case ResultadoAdministradorInicial.Creado:
                logger.LogInformation("Se creó el administrador inicial. Deberá cambiar su contraseña al iniciar sesión.");
                break;
            case ResultadoAdministradorInicial.FaltaConfiguracion:
                logger.LogWarning("No hay usuarios y falta AdminInicial__Usuario, AdminInicial__NombreCompleto o AdminInicial__Contrasena: no se creó el administrador inicial.");
                break;
            case ResultadoAdministradorInicial.ConfiguracionInvalida:
                logger.LogWarning("Los datos de AdminInicial__* no cumplen las reglas (usuario, nombre o política de contraseñas): no se creó el administrador inicial.");
                break;
        }
    }
}
