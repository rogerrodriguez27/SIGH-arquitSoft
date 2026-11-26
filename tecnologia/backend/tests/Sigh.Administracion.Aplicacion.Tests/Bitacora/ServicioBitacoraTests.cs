using Sigh.Administracion.Aplicacion.Bitacora;
using Sigh.Administracion.Aplicacion.Publico;
using Sigh.Administracion.Aplicacion.Tests.Dobles;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Infraestructura.Persistencia;

namespace Sigh.Administracion.Aplicacion.Tests.Bitacora;

/// <summary>ServicioBitacora, contrato público de FR-018 (contracts/servicio-bitacora.md).</summary>
public sealed class ServicioBitacoraTests
{
    private readonly Escenario _e = new();
    private readonly RepositorioBitacoraEnMemoria _bitacora = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();

    private IServicioBitacora Servicio() => new ServicioBitacora(_bitacora, _e.Usuarios, _unidad, _e.Reloj);

    private async Task<RegistroAcceso> RegistroDeMedicoAsync(string accion = "REGISTRAR_ATENCION", string modulo = "Hce")
    {
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);
        return new RegistroAcceso(medico.Id, "Medico", accion, Guid.NewGuid(), modulo);
    }

    [Fact]
    public async Task Registrar_un_acceso_sin_transaccion_activa_lanza_InvalidOperationException()
    {
        var registro = await RegistroDeMedicoAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Servicio().RegistrarAccesoAsync(registro));
        Assert.Empty(_bitacora.Entradas);
    }

    [Fact]
    public async Task Dentro_de_una_transaccion_guarda_el_acceso_permitido_con_nombre_y_hora_del_servidor()
    {
        var registro = await RegistroDeMedicoAsync();

        await _unidad.EjecutarEnTransaccionAsync(() => Servicio().RegistrarAccesoAsync(registro));

        var entrada = Assert.Single(_bitacora.Entradas);
        Assert.Equal(registro.UsuarioId, entrada.UsuarioId);
        Assert.Equal("med01", entrada.NombreUsuario);
        Assert.Equal(CodigoRol.Medico, entrada.Rol);
        Assert.Equal("REGISTRAR_ATENCION", entrada.Accion);
        Assert.Equal(registro.HistoriaId, entrada.HistoriaId);
        Assert.Equal("Hce", entrada.ModuloOrigen);
        Assert.Equal(ResultadoAcceso.Permitido, entrada.Resultado);
        Assert.Equal(_e.Reloj.Ahora, entrada.FechaHora);
        Assert.False(_bitacora.EnTransaccionPropia(entrada.Id));
    }

    [Fact]
    public async Task Un_intento_denegado_se_guarda_en_una_transaccion_propia_aunque_no_haya_unidad_de_trabajo()
    {
        var registro = await RegistroDeMedicoAsync("CONSULTAR_HISTORIA");

        await Servicio().RegistrarIntentoDenegadoAsync(registro);

        var entrada = Assert.Single(_bitacora.Entradas);
        Assert.Equal(ResultadoAcceso.Denegado, entrada.Resultado);
        Assert.True(_bitacora.EnTransaccionPropia(entrada.Id));
    }

    [Theory]
    [InlineData("BORRAR_HISTORIA", "Hce", "Medico")]
    [InlineData("CONSULTAR_HISTORIA", "Administracion", "Medico")]
    [InlineData("CONSULTAR_HISTORIA", "Hce", "Superusuario")]
    public async Task Valores_fuera_de_las_listas_cerradas_lanzan_ArgumentException(string accion, string modulo, string rol)
    {
        var medico = await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);
        var registro = new RegistroAcceso(medico.Id, rol, accion, Guid.NewGuid(), modulo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _unidad.EjecutarEnTransaccionAsync(() => Servicio().RegistrarAccesoAsync(registro)));
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio().RegistrarIntentoDenegadoAsync(registro));
        Assert.Empty(_bitacora.Entradas);
    }

    [Fact]
    public async Task Un_usuario_inexistente_lanza_ArgumentException()
    {
        var registro = new RegistroAcceso(Guid.NewGuid(), "Medico", "CONSULTAR_HISTORIA", Guid.NewGuid(), "Hce");

        await Assert.ThrowsAsync<ArgumentException>(() => Servicio().RegistrarIntentoDenegadoAsync(registro));
    }

    [Fact]
    public void El_contrato_no_tiene_campos_para_contenido_clinico_ni_fecha()
    {
        Assert.Equal(["Accion", "HistoriaId", "ModuloOrigen", "Rol", "UsuarioId"],
            typeof(RegistroAcceso).GetProperties().Select(p => p.Name).Order());
    }
}
