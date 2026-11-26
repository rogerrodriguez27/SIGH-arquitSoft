using Sigh.Administracion.Aplicacion.AdministradorInicial;
using Sigh.Administracion.Aplicacion.Tests.Dobles;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Aplicacion.Tests.AdministradorInicial;

/// <summary>CrearAdministradorInicialCasoUso (FR-016, research D7).</summary>
public sealed class CrearAdministradorInicialCasoUsoTests
{
    private readonly Escenario _e = new();

    private CrearAdministradorInicialCasoUso CasoUso() => new(_e.Usuarios, _e.Eventos, _e.Hasher, _e.Reloj);

    [Fact]
    public async Task Sin_usuarios_y_con_las_tres_variables_crea_un_administrador_con_cambio_pendiente()
    {
        var resultado = await CasoUso().EjecutarAsync(new("admin", "Administrador Ficticio", "Inicial2026"));

        Assert.Equal(ResultadoAdministradorInicial.Creado, resultado);
        var admin = Assert.Single(_e.Usuarios.Usuarios);
        Assert.Equal([CodigoRol.Administrador], admin.Roles);
        Assert.True(admin.CambioContrasenaPendiente);
    }

    [Theory]
    [InlineData(null, "Administrador Ficticio", "Inicial2026")]
    [InlineData("admin", null, "Inicial2026")]
    [InlineData("admin", "Administrador Ficticio", null)]
    [InlineData("admin", "Administrador Ficticio", "")]
    public async Task Si_falta_una_variable_no_crea_nada(string? usuario, string? nombre, string? contrasena)
    {
        var resultado = await CasoUso().EjecutarAsync(new(usuario, nombre, contrasena));

        Assert.Equal(ResultadoAdministradorInicial.FaltaConfiguracion, resultado);
        Assert.Empty(_e.Usuarios.Usuarios);
    }

    [Fact]
    public async Task Si_la_contrasena_no_cumple_la_politica_no_crea_nada()
    {
        var resultado = await CasoUso().EjecutarAsync(new("admin", "Administrador Ficticio", "corta"));

        Assert.Equal(ResultadoAdministradorInicial.ConfiguracionInvalida, resultado);
        Assert.Empty(_e.Usuarios.Usuarios);
    }

    [Fact]
    public async Task Si_ya_hay_usuarios_no_hace_nada()
    {
        await _e.AgregarUsuarioAsync("med01", "Clave2026", CodigoRol.Medico);

        var resultado = await CasoUso().EjecutarAsync(new("admin", "Administrador Ficticio", "Inicial2026"));

        Assert.Equal(ResultadoAdministradorInicial.YaExistenUsuarios, resultado);
        Assert.Single(_e.Usuarios.Usuarios);
    }

    [Fact]
    public void Los_datos_no_muestran_la_contrasena_al_convertirse_en_texto()
    {
        Assert.DoesNotContain("Inicial2026", new DatosAdministradorInicial("admin", "Admin", "Inicial2026").ToString());
    }
}
