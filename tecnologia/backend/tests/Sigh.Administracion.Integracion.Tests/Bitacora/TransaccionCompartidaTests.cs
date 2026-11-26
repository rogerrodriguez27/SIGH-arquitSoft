using Sigh.Administracion.Aplicacion.Publico;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Integracion.Tests.Bitacora;

/// <summary>
/// FR-022 con PostgreSQL real: la escritura de otro módulo y su registro en la bitácora se confirman o
/// se revierten juntos; los intentos denegados quedan aunque la operación se revierta (FR-020, SC-002).
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Category", "Integracion")]
public sealed class TransaccionCompartidaTests(PostgresFixture bd) : IAsyncLifetime
{
    private Guid _medicoId;

    private sealed class HasherSimple : IHasherContrasenas
    {
        public string Calcular(string contrasena) => "hash:" + contrasena;

        public ResultadoVerificacionContrasena Verificar(string hash, string contrasena) =>
            ResultadoVerificacionContrasena.Incorrecta;
    }

    public async Task InitializeAsync()
    {
        await using (var comando = bd.FuenteDatos.CreateCommand("""
            CREATE SCHEMA IF NOT EXISTS modulo_simulado;
            CREATE TABLE IF NOT EXISTS modulo_simulado.registros (id uuid PRIMARY KEY, texto text NOT NULL);
            """))
        {
            await comando.ExecuteNonQueryAsync();
        }

        var medico = Usuario.Crear($"med{Guid.NewGuid():N}"[..20], "Médico Ficticio",
            ContrasenaPropuesta.Crear("Clave2026"), [CodigoRol.Medico], new HasherSimple(), new RelojSistema());
        await using var contexto = bd.CrearContexto();
        await new RepositorioUsuariosEfCore(contexto).AgregarAsync(medico);
        _medicoId = medico.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private RegistroAcceso Registro(Guid historiaId, string accion = "REGISTRAR_ATENCION") =>
        new(_medicoId, "Medico", accion, historiaId, "Hce");

    private async Task<long> ContarAsync(string sql, Guid id)
    {
        await using var comando = bd.FuenteDatos.CreateCommand(sql);
        comando.Parameters.AddWithValue(id);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private Task<long> EntradasDeHistoriaAsync(Guid historiaId) =>
        ContarAsync("SELECT count(*) FROM administracion.bitacora_accesos_hce WHERE historia_id = $1", historiaId);

    private Task<long> RegistrosDelModuloAsync(Guid id) =>
        ContarAsync("SELECT count(*) FROM modulo_simulado.registros WHERE id = $1", id);

    [Fact]
    public async Task Si_ambas_escrituras_funcionan_se_confirman_juntas()
    {
        var historia = Guid.NewGuid();
        var registro = new RegistroSimulado { Texto = "atención ficticia" };

        await using (var peticion = new PeticionSimulada(bd))
        {
            await peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(async () =>
            {
                peticion.Modulo.Registros.Add(registro);
                await peticion.Modulo.SaveChangesAsync();
                await peticion.Bitacora.RegistrarAccesoAsync(Registro(historia));
            });
        }

        Assert.Equal(1, await RegistrosDelModuloAsync(registro.Id));
        Assert.Equal(1, await EntradasDeHistoriaAsync(historia));
    }

    [Fact]
    public async Task Si_la_bitacora_falla_la_escritura_del_modulo_se_revierte()
    {
        var historia = Guid.NewGuid();
        var registro = new RegistroSimulado { Texto = "atención ficticia" };

        await using (var peticion = new PeticionSimulada(bd))
        {
            await Assert.ThrowsAsync<ArgumentException>(() => peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(async () =>
            {
                peticion.Modulo.Registros.Add(registro);
                await peticion.Modulo.SaveChangesAsync();
                await peticion.Bitacora.RegistrarAccesoAsync(Registro(historia, accion: "ACCION_INEXISTENTE"));
            }));
        }

        Assert.Equal(0, await RegistrosDelModuloAsync(registro.Id));
        Assert.Equal(0, await EntradasDeHistoriaAsync(historia));
    }

    [Fact]
    public async Task Si_el_modulo_falla_despues_de_registrar_la_bitacora_tambien_se_revierte()
    {
        var historia = Guid.NewGuid();

        await using (var peticion = new PeticionSimulada(bd))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(async () =>
            {
                await peticion.Bitacora.RegistrarAccesoAsync(Registro(historia));
                throw new InvalidOperationException("Falla simulada del módulo.");
            }));
        }

        Assert.Equal(0, await EntradasDeHistoriaAsync(historia));
    }

    [Fact]
    public async Task Un_intento_denegado_queda_guardado_aunque_la_operacion_se_revierta()
    {
        var historia = Guid.NewGuid();
        var registro = new RegistroSimulado { Texto = "no debe quedar" };

        await using (var peticion = new PeticionSimulada(bd))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(async () =>
            {
                peticion.Modulo.Registros.Add(registro);
                await peticion.Modulo.SaveChangesAsync();
                await peticion.Bitacora.RegistrarIntentoDenegadoAsync(Registro(historia, "CONSULTAR_HISTORIA"));
                throw new InvalidOperationException("La operación se rechaza.");
            }));
        }

        Assert.Equal(0, await RegistrosDelModuloAsync(registro.Id));
        Assert.Equal(1, await ContarAsync(
            "SELECT count(*) FROM administracion.bitacora_accesos_hce WHERE historia_id = $1 AND resultado = 'Denegado'", historia));
    }

    [Fact]
    public async Task Sin_transaccion_activa_el_registro_de_un_acceso_se_rechaza()
    {
        var historia = Guid.NewGuid();
        await using var peticion = new PeticionSimulada(bd);

        await Assert.ThrowsAsync<InvalidOperationException>(() => peticion.Bitacora.RegistrarAccesoAsync(Registro(historia)));
        Assert.Equal(0, await EntradasDeHistoriaAsync(historia));
    }

    [Fact]
    public async Task La_misma_peticion_puede_usar_varias_transacciones_seguidas()
    {
        var primera = Guid.NewGuid();
        var segunda = Guid.NewGuid();
        await using var peticion = new PeticionSimulada(bd);

        await peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(() => peticion.Bitacora.RegistrarAccesoAsync(Registro(primera)));
        await peticion.UnidadDeTrabajo.EjecutarEnTransaccionAsync(() => peticion.Bitacora.RegistrarAccesoAsync(Registro(segunda)));

        Assert.Equal(1, await EntradasDeHistoriaAsync(primera));
        Assert.Equal(1, await EntradasDeHistoriaAsync(segunda));
    }
}
