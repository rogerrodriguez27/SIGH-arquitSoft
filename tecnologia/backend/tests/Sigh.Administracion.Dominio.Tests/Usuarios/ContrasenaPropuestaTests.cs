using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Tests.Dobles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Dominio.Tests.Usuarios;

/// <summary>Política de contraseñas (FR-005) y cambio de contraseña propia (FR-006).</summary>
public sealed class ContrasenaPropuestaTests
{
    [Theory]
    [InlineData("Clave2026")]
    [InlineData("abcdefg1")]
    [InlineData("12345678a")]
    public void Acepta_8_o_mas_caracteres_con_letras_y_numeros(string texto)
    {
        Assert.Equal(texto, ContrasenaPropuesta.Crear(texto).Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc123")]           // menos de 8
    [InlineData("abcdefgh")]         // sin números
    [InlineData("12345678")]         // sin letras
    public void Rechaza_contrasenas_que_no_cumplen_la_politica(string texto)
    {
        Assert.Throws<ReglaNegocioError>(() => ContrasenaPropuesta.Crear(texto));
    }

    [Fact]
    public void Rechaza_contrasenas_de_mas_de_128_caracteres()
    {
        Assert.Throws<ReglaNegocioError>(() => ContrasenaPropuesta.Crear(new string('a', 128) + "1"));
    }

    [Fact]
    public void CambiarContrasena_con_la_actual_correcta_quita_el_cambio_pendiente()
    {
        var hasher = new HasherFalso();
        var usuario = Usuario.Crear("med01", "Médico Ficticio", ContrasenaPropuesta.Crear("Inicial2026"),
            [CodigoRol.Medico], hasher, RelojFijo.Nuevo());

        usuario.CambiarContrasena("Inicial2026", ContrasenaPropuesta.Crear("Nueva2026x"), hasher);

        Assert.False(usuario.CambioContrasenaPendiente);
        Assert.Equal("hash:Nueva2026x", usuario.HashContrasena);
    }

    [Fact]
    public void CambiarContrasena_con_la_actual_incorrecta_se_rechaza()
    {
        var hasher = new HasherFalso();
        var usuario = Usuario.Crear("med01", "Médico Ficticio", ContrasenaPropuesta.Crear("Inicial2026"),
            [CodigoRol.Medico], hasher, RelojFijo.Nuevo());

        Assert.Throws<ReglaNegocioError>(() =>
            usuario.CambiarContrasena("Otra2026", ContrasenaPropuesta.Crear("Nueva2026x"), hasher));
        Assert.True(usuario.CambioContrasenaPendiente);
    }
}
