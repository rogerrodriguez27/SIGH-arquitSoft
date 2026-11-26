using Npgsql;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Integracion.Tests.Bitacora;

/// <summary>
/// Nadie, ni el administrador ni SQL manual, puede modificar o eliminar la bitácora (FR-021, SC-003,
/// historia 4, escenario 3).
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Category", "Integracion")]
public sealed class BitacoraInmutableTests(PostgresFixture bd)
{
    private async Task<Guid> InsertarAsync()
    {
        var entrada = EntradaBitacora.Crear(Guid.NewGuid(), "med01", CodigoRol.Medico, AccionBitacora.ConsultarHistoria,
            Guid.NewGuid(), ModuloOrigen.Hce, ResultadoAcceso.Permitido, new RelojSistema());
        await using var contexto = bd.CrearContexto();
        contexto.EntradasBitacora.Add(entrada);
        await contexto.SaveChangesAsync();
        return entrada.Id;
    }

    private async Task EjecutarAsync(string sql, Guid? id = null)
    {
        await using var comando = bd.FuenteDatos.CreateCommand(sql);
        if (id is { } valor) comando.Parameters.AddWithValue(valor);
        await comando.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task UPDATE_se_rechaza()
    {
        var id = await InsertarAsync();

        await Assert.ThrowsAsync<PostgresException>(() =>
            EjecutarAsync("UPDATE administracion.bitacora_accesos_hce SET resultado = 'Denegado' WHERE id = $1", id));
    }

    [Fact]
    public async Task DELETE_se_rechaza()
    {
        var id = await InsertarAsync();

        await Assert.ThrowsAsync<PostgresException>(() =>
            EjecutarAsync("DELETE FROM administracion.bitacora_accesos_hce WHERE id = $1", id));
    }

    [Fact]
    public async Task TRUNCATE_se_rechaza()
    {
        await InsertarAsync();

        await Assert.ThrowsAsync<PostgresException>(() => EjecutarAsync("TRUNCATE administracion.bitacora_accesos_hce"));
    }

    [Fact]
    public async Task Un_DELETE_que_no_afecta_filas_tambien_se_rechaza()
    {
        await Assert.ThrowsAsync<PostgresException>(() =>
            EjecutarAsync("DELETE FROM administracion.bitacora_accesos_hce WHERE id = $1", Guid.NewGuid()));
    }

    [Fact]
    public async Task Existen_los_indices_de_consulta()
    {
        await using var comando = bd.FuenteDatos.CreateCommand(
            "SELECT count(*) FROM pg_indexes WHERE schemaname = 'administracion' AND tablename = 'bitacora_accesos_hce' AND indexname LIKE 'ix_%'");

        Assert.Equal(3L, await comando.ExecuteScalarAsync());
    }
}
