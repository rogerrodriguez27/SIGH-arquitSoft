using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Api.Tests.Soporte;

namespace Sigh.Api.Tests.Seguridad;

/// <summary>
/// SC-008 y FR-023: ninguna contraseña, token ni clave de firma aparece en los logs de la API ni en los
/// eventos de seguridad, ni siquiera en el nivel Trace.
/// </summary>
public sealed class SinSecretosEnLogsTests : IDisposable
{
    private const string Inicial = "Inicial2026Sec";
    private const string Nueva = "Nueva2026Sec";
    private const string Incorrecta = "Incorrecta2026Sec";
    private const string Temporal = "Temporal2026Sec";
    private readonly FabricaApiPruebas _fabrica = new();

    public void Dispose() => _fabrica.Dispose();

    private async Task<(HttpStatusCode Estado, string? Token)> IniciarAsync(string usuario, string contrasena)
    {
        var respuesta = await _fabrica.CreateClient().PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new { nombreUsuario = usuario, contrasena });
        if (respuesta.StatusCode != HttpStatusCode.OK)
            return (respuesta.StatusCode, null);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (respuesta.StatusCode, cuerpo.GetProperty("token").GetString());
    }

    [Fact]
    public async Task Inicio_cambio_y_restablecimiento_de_contrasena_no_dejan_secretos_en_los_logs()
    {
        var medico = await _fabrica.CrearUsuarioAsync("med01", Inicial, [CodigoRol.Medico], cambioPendiente: true);
        await _fabrica.CrearUsuarioAsync("admin", Inicial, [CodigoRol.Administrador]);
        var tokens = new List<string>();

        // Inicio fallido, con usuario inexistente y exitoso.
        Assert.Equal(HttpStatusCode.Unauthorized, (await IniciarAsync("med01", Incorrecta)).Estado);
        Assert.Equal(HttpStatusCode.Unauthorized, (await IniciarAsync("noexiste", Incorrecta)).Estado);
        var (_, tokenMedico) = await IniciarAsync("med01", Inicial);
        tokens.Add(tokenMedico!);

        // Cambio de contraseña obligatorio: con la actual incorrecta y luego correcta.
        var medicoCliente = _fabrica.ClienteConToken(tokenMedico!);
        await medicoCliente.PostAsJsonAsync("/api/v1/autenticacion/cambiar-contrasena",
            new { contrasenaActual = Incorrecta, contrasenaNueva = Nueva });
        var cambio = await medicoCliente.PostAsJsonAsync("/api/v1/autenticacion/cambiar-contrasena",
            new { contrasenaActual = Inicial, contrasenaNueva = Nueva });
        tokens.Add((await cambio.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!);

        // Restablecimiento por el administrador y alta de un usuario con contraseña inicial.
        var (_, tokenAdmin) = await IniciarAsync("admin", Inicial);
        tokens.Add(tokenAdmin!);
        var admin = _fabrica.ClienteConToken(tokenAdmin!);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync(
            $"/api/v1/usuarios/{medico.Id}/restablecer-contrasena", new { contrasenaTemporal = Temporal })).StatusCode);
        await admin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nombreUsuario = "farm01", nombreCompleto = "Farmacéutico Ficticio", contrasenaInicial = Inicial,
            roles = new[] { "Farmaceutico" },
        });

        var secretos = new[] { Inicial, Nueva, Incorrecta, Temporal, _fabrica.ClaveFirma }.Concat(tokens).ToList();
        var logs = _fabrica.Logs.Lineas;
        Assert.NotEmpty(logs);
        foreach (var secreto in secretos)
        {
            Assert.DoesNotContain(logs, linea => linea.Contains(secreto, StringComparison.Ordinal));
            Assert.DoesNotContain(_fabrica.Eventos.Eventos, e => e.Detalle.Contains(secreto, StringComparison.Ordinal));
        }
    }
}
