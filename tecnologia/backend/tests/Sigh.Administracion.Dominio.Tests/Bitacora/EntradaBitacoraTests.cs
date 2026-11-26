using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Tests.Dobles;

namespace Sigh.Administracion.Dominio.Tests.Bitacora;

/// <summary>EntradaBitacora (data-model.md, RF24, RN-06, FR-019, FR-021, FR-023).</summary>
public sealed class EntradaBitacoraTests
{
    private static EntradaBitacora Crear(string accion = AccionBitacora.ConsultarHistoria,
        string modulo = ModuloOrigen.Hce, ResultadoAcceso resultado = ResultadoAcceso.Permitido) =>
        EntradaBitacora.Crear(Guid.NewGuid(), "med01", CodigoRol.Medico, accion, Guid.NewGuid(), modulo,
            resultado, RelojFijo.Nuevo());

    [Fact]
    public void Guarda_todos_los_datos_con_la_hora_del_servidor()
    {
        var reloj = RelojFijo.Nuevo();
        var usuario = Guid.NewGuid();
        var historia = Guid.NewGuid();

        var entrada = EntradaBitacora.Crear(usuario, "med01", CodigoRol.Medico, AccionBitacora.RegistrarAtencion,
            historia, ModuloOrigen.Hce, ResultadoAcceso.Permitido, reloj);

        Assert.NotEqual(Guid.Empty, entrada.Id);
        Assert.Equal(usuario, entrada.UsuarioId);
        Assert.Equal("med01", entrada.NombreUsuario);
        Assert.Equal(CodigoRol.Medico, entrada.Rol);
        Assert.Equal(reloj.Ahora, entrada.FechaHora);
        Assert.Equal("REGISTRAR_ATENCION", entrada.Accion);
        Assert.Equal(historia, entrada.HistoriaId);
        Assert.Equal("Hce", entrada.ModuloOrigen);
        Assert.Equal(ResultadoAcceso.Permitido, entrada.Resultado);
    }

    [Theory]
    [InlineData("CONSULTAR_HISTORIA")]
    [InlineData("REGISTRAR_ATENCION")]
    [InlineData("AGREGAR_NOTA_CORRECCION")]
    [InlineData("REGISTRAR_RESULTADO")]
    [InlineData("DISPENSAR_RECETA")]
    [InlineData("REGISTRAR_INGRESO")]
    [InlineData("REGISTRAR_TRIAJE")]
    public void Acepta_las_acciones_de_la_lista_cerrada(string accion)
    {
        Assert.Equal(accion, Crear(accion: accion).Accion);
    }

    [Theory]
    [InlineData("BORRAR_HISTORIA")]
    [InlineData("consultar_historia")]
    [InlineData("")]
    public void Rechaza_acciones_fuera_de_la_lista_cerrada(string accion)
    {
        Assert.Throws<ArgumentException>(() => Crear(accion: accion));
    }

    [Theory]
    [InlineData("Hce")]
    [InlineData("Admision")]
    [InlineData("Farmacia")]
    [InlineData("Laboratorio")]
    public void Acepta_los_modulos_de_origen_de_la_lista_cerrada(string modulo)
    {
        Assert.Equal(modulo, Crear(modulo: modulo).ModuloOrigen);
    }

    [Theory]
    [InlineData("Administracion")]
    [InlineData("hce")]
    public void Rechaza_modulos_fuera_de_la_lista_cerrada(string modulo)
    {
        Assert.Throws<ArgumentException>(() => Crear(modulo: modulo));
    }

    [Fact]
    public void Registra_tambien_los_intentos_denegados()
    {
        Assert.Equal(ResultadoAcceso.Denegado, Crear(resultado: ResultadoAcceso.Denegado).Resultado);
    }

    [Fact]
    public void No_tiene_metodos_de_modificacion_ni_campos_clinicos()
    {
        var tipo = typeof(EntradaBitacora);

        Assert.DoesNotContain(tipo.GetProperties(), p => p.SetMethod?.IsPublic == true);
        Assert.DoesNotContain(tipo.GetMethods(), m => m.DeclaringType == tipo && !m.IsStatic && !m.IsSpecialName);
        Assert.Equal(
            ["Accion", "FechaHora", "HistoriaId", "Id", "ModuloOrigen", "NombreUsuario", "Resultado", "Rol", "UsuarioId"],
            tipo.GetProperties().Select(p => p.Name).Order());
    }
}
