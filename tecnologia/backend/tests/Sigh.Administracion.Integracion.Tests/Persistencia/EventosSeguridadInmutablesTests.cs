using Npgsql;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Integracion.Tests.Persistencia;

/// <summary>Los eventos de seguridad no se pueden modificar ni eliminar, ni con SQL manual (FR-021, SC-003).</summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Category", "Integracion")]
public sealed class EventosSeguridadInmutablesTests(PostgresFixture bd)
{
    private async Task<Guid> InsertarEventoAsync()
    {
        var evento = EventoSeguridad.Crear(TipoEventoSeguridad.InicioSesionExitoso, new RelojSistema(), Guid.NewGuid());
        await using var contexto = bd.CrearContexto();
        contexto.EventosSeguridad.Add(evento);
        await contexto.SaveChangesAsync();
        return evento.Id;
    }

    private async Task EjecutarAsync(string sql, Guid id)
    {
        await using var comando = bd.FuenteDatos.CreateCommand(sql);
        comando.Parameters.AddWithValue(id);
        await comando.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Se_pueden_insertar_eventos()
    {
        var id = await InsertarEventoAsync();

        await using var comando = bd.FuenteDatos.CreateCommand(
            "SELECT count(*) FROM administracion.eventos_seguridad WHERE id = $1");
        comando.Parameters.AddWithValue(id);
        Assert.Equal(1L, await comando.ExecuteScalarAsync());
    }

    [Fact]
    public async Task UPDATE_se_rechaza()
    {
        var id = await InsertarEventoAsync();

        await Assert.ThrowsAsync<PostgresException>(() =>
            EjecutarAsync("UPDATE administracion.eventos_seguridad SET detalle = 'alterado' WHERE id = $1", id));
    }

    [Fact]
    public async Task DELETE_se_rechaza()
    {
        var id = await InsertarEventoAsync();

        await Assert.ThrowsAsync<PostgresException>(() =>
            EjecutarAsync("DELETE FROM administracion.eventos_seguridad WHERE id = $1", id));
    }

    [Fact]
    public async Task Un_UPDATE_que_no_afecta_filas_tambien_se_rechaza()
    {
        await Assert.ThrowsAsync<PostgresException>(() =>
            EjecutarAsync("UPDATE administracion.eventos_seguridad SET detalle = 'x' WHERE id = $1", Guid.NewGuid()));
    }

    [Fact]
    public async Task TRUNCATE_se_rechaza()
    {
        await InsertarEventoAsync();

        await using var comando = bd.FuenteDatos.CreateCommand("TRUNCATE administracion.eventos_seguridad");
        await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());
    }
}
