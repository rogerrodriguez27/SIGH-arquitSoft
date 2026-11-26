using Sigh.Administracion.Aplicacion.Bitacora.ConsultarBitacora;
using Sigh.Administracion.Aplicacion.Tests.Dobles;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Aplicacion.Tests.Bitacora;

/// <summary>ConsultarBitacoraCasoUso (HU16, FR-024, FR-025).</summary>
public sealed class ConsultarBitacoraCasoUsoTests
{
    private readonly Escenario _e = new();
    private readonly RepositorioBitacoraEnMemoria _bitacora = new();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _medico = Guid.NewGuid();
    private readonly Guid _farmaceutico = Guid.NewGuid();
    private readonly Guid _historiaA = Guid.NewGuid();
    private readonly Guid _historiaB = Guid.NewGuid();

    private ConsultarBitacoraCasoUso CasoUso() => new(_bitacora, _e.Eventos, _e.Reloj);

    private DateTimeOffset Hoy => _e.Reloj.Ahora;

    /// <summary>Entradas ficticias: una por hora, desde hace 10 horas.</summary>
    private async Task PrecargarAsync()
    {
        var reloj = new RelojFijo(Hoy.AddHours(-10));
        for (var i = 0; i < 10; i++)
        {
            var esMedico = i % 2 == 0;
            await _bitacora.AgregarAsync(EntradaBitacora.Crear(
                esMedico ? _medico : _farmaceutico,
                esMedico ? "med01" : "farm01",
                esMedico ? CodigoRol.Medico : CodigoRol.Farmaceutico,
                esMedico ? AccionBitacora.ConsultarHistoria : AccionBitacora.DispensarReceta,
                i < 5 ? _historiaA : _historiaB,
                esMedico ? ModuloOrigen.Hce : ModuloOrigen.Farmacia,
                esMedico ? ResultadoAcceso.Permitido : ResultadoAcceso.Denegado,
                reloj));
            reloj.Avanzar(TimeSpan.FromHours(1));
        }
    }

    private ConsultarBitacoraConsulta Consulta(DateTimeOffset? desde = null, DateTimeOffset? hasta = null,
        Guid? usuario = null, Guid? historia = null, string? accion = null, ResultadoAcceso? resultado = null,
        int pagina = 1, int tamano = 20) =>
        new(_admin, desde ?? Hoy.AddDays(-1), hasta ?? Hoy, usuario, historia, accion, resultado, pagina, tamano);

    [Fact]
    public async Task Filtra_por_usuario_y_rango_y_ordena_de_la_mas_reciente_a_la_mas_antigua()
    {
        await PrecargarAsync();

        var pagina = await CasoUso().EjecutarAsync(Consulta(desde: Hoy.AddHours(-10), usuario: _medico));

        Assert.Equal(5, pagina.Total);
        Assert.All(pagina.Elementos, e => Assert.Equal(_medico, e.UsuarioId));
        Assert.Equal(pagina.Elementos.Select(e => e.FechaHora).OrderByDescending(f => f), pagina.Elementos.Select(e => e.FechaHora));
    }

    [Fact]
    public async Task Combina_todos_los_filtros()
    {
        await PrecargarAsync();

        var pagina = await CasoUso().EjecutarAsync(Consulta(usuario: _farmaceutico, historia: _historiaB,
            accion: AccionBitacora.DispensarReceta, resultado: ResultadoAcceso.Denegado));

        Assert.Equal(3, pagina.Total);
        Assert.All(pagina.Elementos, e =>
        {
            Assert.Equal(_historiaB, e.HistoriaId);
            Assert.Equal(ResultadoAcceso.Denegado, e.Resultado);
        });
    }

    [Fact]
    public async Task El_rango_de_fechas_es_inclusivo_y_excluye_lo_que_queda_fuera()
    {
        await PrecargarAsync();

        var pagina = await CasoUso().EjecutarAsync(Consulta(desde: Hoy.AddHours(-3), hasta: Hoy.AddHours(-2)));

        Assert.Equal(2, pagina.Total);
    }

    [Fact]
    public async Task Pagina_los_resultados()
    {
        await PrecargarAsync();

        var segunda = await CasoUso().EjecutarAsync(Consulta(pagina: 2, tamano: 4));

        Assert.Equal(10, segunda.Total);
        Assert.Equal(4, segunda.Elementos.Count);
        Assert.Equal(2, segunda.NumeroPagina);
    }

    [Fact]
    public async Task Cada_consulta_registra_BITACORA_CONSULTADA_con_los_filtros()
    {
        await CasoUso().EjecutarAsync(Consulta(usuario: _medico, resultado: ResultadoAcceso.Denegado));

        var evento = Assert.Single(_e.Eventos.Eventos);
        Assert.Equal(TipoEventoSeguridad.BitacoraConsultada, evento.Tipo);
        Assert.Equal(_admin, evento.ActorId);
        Assert.StartsWith("filtros:", evento.Detalle);
        Assert.Contains($"usuario={_medico}", evento.Detalle);
        Assert.Contains("resultado=Denegado", evento.Detalle);
    }

    [Fact]
    public async Task Rechaza_un_rango_invertido()
    {
        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            CasoUso().EjecutarAsync(Consulta(desde: Hoy, hasta: Hoy.AddDays(-1))));
        Assert.Empty(_e.Eventos.Eventos);
    }

    [Fact]
    public async Task Rechaza_un_rango_mayor_a_un_anio()
    {
        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            CasoUso().EjecutarAsync(Consulta(desde: Hoy.AddYears(-1).AddSeconds(-1), hasta: Hoy)));
    }

    [Fact]
    public async Task Acepta_un_rango_de_exactamente_un_anio()
    {
        var pagina = await CasoUso().EjecutarAsync(Consulta(desde: Hoy.AddYears(-1), hasta: Hoy));

        Assert.Equal(0, pagina.Total);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Rechaza_paginacion_invalida(int numeroPagina, int tamano)
    {
        await Assert.ThrowsAsync<ReglaNegocioError>(() =>
            CasoUso().EjecutarAsync(Consulta(pagina: numeroPagina, tamano: tamano)));
    }

    [Fact]
    public async Task Rechaza_una_accion_fuera_de_la_lista_cerrada()
    {
        await Assert.ThrowsAsync<ReglaNegocioError>(() => CasoUso().EjecutarAsync(Consulta(accion: "BORRAR_TODO")));
    }
}
