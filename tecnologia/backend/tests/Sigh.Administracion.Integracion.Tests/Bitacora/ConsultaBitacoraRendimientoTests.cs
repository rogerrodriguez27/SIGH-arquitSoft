using System.Diagnostics;
using Npgsql;
using NpgsqlTypes;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Xunit.Abstractions;

namespace Sigh.Administracion.Integracion.Tests.Bitacora;

/// <summary>
/// SC-006 (research D8): con al menos 100 000 entradas ficticias, el 95 % de las consultas filtradas de
/// la bitácora responde en 2 segundos o menos. Usa una base propia para no mezclar con otras pruebas.
/// </summary>
[Trait("Category", "Integracion")]
public sealed class ConsultaBitacoraRendimientoTests : IClassFixture<PostgresFixture>
{
    private const int Entradas = 100_000;
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(2);
    private readonly PostgresFixture _bd;
    private readonly Guid[] _usuarios = Enumerable.Range(0, 300).Select(_ => Guid.NewGuid()).ToArray();
    private readonly Guid[] _historias = Enumerable.Range(0, 5_000).Select(_ => Guid.NewGuid()).ToArray();
    private readonly DateTimeOffset _ahora = DateTimeOffset.UtcNow;

    private readonly ITestOutputHelper _salida;

    public ConsultaBitacoraRendimientoTests(PostgresFixture bd, ITestOutputHelper salida)
    {
        _bd = bd;
        _salida = salida;
    }

    /// <summary>Carga masiva con COPY binario: un año de accesos ficticios (principio IX).</summary>
    private async Task PrecargarAsync()
    {
        var acciones = AccionBitacora.Todas.ToArray();
        var aleatorio = new Random(2026);
        await using var conexion = await _bd.FuenteDatos.OpenConnectionAsync();
        await using (var importador = await conexion.BeginBinaryImportAsync(
            "COPY administracion.bitacora_accesos_hce (id, usuario_id, nombre_usuario, rol, fecha_hora, accion, historia_id, modulo_origen, resultado) FROM STDIN (FORMAT BINARY)"))
        {
            for (var i = 0; i < Entradas; i++)
            {
                var usuario = aleatorio.Next(_usuarios.Length);
                await importador.StartRowAsync();
                await importador.WriteAsync(Guid.NewGuid(), NpgsqlDbType.Uuid);
                await importador.WriteAsync(_usuarios[usuario], NpgsqlDbType.Uuid);
                await importador.WriteAsync($"usuario{usuario:000}", NpgsqlDbType.Varchar);
                await importador.WriteAsync("Medico", NpgsqlDbType.Varchar);
                await importador.WriteAsync(_ahora.AddMinutes(-aleatorio.Next(525_600)), NpgsqlDbType.TimestampTz);
                await importador.WriteAsync(acciones[aleatorio.Next(acciones.Length)], NpgsqlDbType.Varchar);
                await importador.WriteAsync(_historias[aleatorio.Next(_historias.Length)], NpgsqlDbType.Uuid);
                await importador.WriteAsync(ModuloOrigen.Hce, NpgsqlDbType.Varchar);
                await importador.WriteAsync(aleatorio.Next(10) == 0 ? "Denegado" : "Permitido", NpgsqlDbType.Varchar);
            }
            await importador.CompleteAsync();
        }

        await using var analizar = conexion.CreateCommand();
        analizar.CommandText = "ANALYZE administracion.bitacora_accesos_hce";
        await analizar.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task El_95_por_ciento_de_las_consultas_responde_en_2_segundos_o_menos()
    {
        await PrecargarAsync();
        var aleatorio = new Random(7);
        var filtros = new List<FiltroBitacora>();
        for (var i = 0; i < 20; i++)
        {
            filtros.Add(new FiltroBitacora(_ahora.AddYears(-1), _ahora));                                          // todo el año
            filtros.Add(new FiltroBitacora(_ahora.AddDays(-30), _ahora, UsuarioId: _usuarios[aleatorio.Next(_usuarios.Length)]));
            filtros.Add(new FiltroBitacora(_ahora.AddYears(-1), _ahora, HistoriaId: _historias[aleatorio.Next(_historias.Length)]));
            filtros.Add(new FiltroBitacora(_ahora.AddDays(-7), _ahora, Accion: AccionBitacora.ConsultarHistoria,
                Resultado: ResultadoAcceso.Denegado));
        }

        var tiempos = new List<TimeSpan>();
        foreach (var filtro in filtros)
        {
            await using var contexto = _bd.CrearContexto();
            var repositorio = new RepositorioBitacoraEfCore(contexto, new FabricaContextoAdministracion(_bd.FuenteDatos));
            var cronometro = Stopwatch.StartNew();
            var pagina = await repositorio.ConsultarAsync(filtro, 1, 20);
            tiempos.Add(cronometro.Elapsed);
            Assert.True(pagina.Elementos.Count <= 20);
        }

        var p95 = tiempos.Order().ElementAt((int)Math.Ceiling(tiempos.Count * 0.95) - 1);
        _salida.WriteLine($"{Entradas} entradas, {tiempos.Count} consultas: p95 = {p95.TotalMilliseconds:F0} ms, máximo = {tiempos.Max().TotalMilliseconds:F0} ms");
        Assert.True(p95 <= Limite, $"p95 = {p95.TotalMilliseconds:F0} ms (límite {Limite.TotalMilliseconds} ms)");
    }
}
