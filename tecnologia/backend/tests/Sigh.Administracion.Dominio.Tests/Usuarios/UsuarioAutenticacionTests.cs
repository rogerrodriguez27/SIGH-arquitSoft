using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Tests.Dobles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Dominio.Tests.Usuarios;

/// <summary>Usuario.Crear e Usuario.IntentarAutenticar (data-model.md, FR-003 a FR-005, FR-010).</summary>
public sealed class UsuarioAutenticacionTests
{
    private const string Contrasena = "Clave2026";
    private readonly HasherFalso _hasher = new();
    private readonly RelojFijo _reloj = RelojFijo.Nuevo();

    private Usuario CrearUsuario(string nombreUsuario = "farm01", params CodigoRol[] roles) =>
        Usuario.Crear(nombreUsuario, "Usuario Ficticio", ContrasenaPropuesta.Crear(Contrasena),
            roles.Length == 0 ? [CodigoRol.Farmaceutico] : roles, _hasher, _reloj);

    // ---------- Crear ----------

    [Fact]
    public void Crear_deja_la_cuenta_activa_con_cambio_de_contrasena_pendiente()
    {
        var usuario = CrearUsuario();

        Assert.Equal(EstadoUsuario.Activo, usuario.Estado);
        Assert.True(usuario.CambioContrasenaPendiente);
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Null(usuario.BloqueadoHasta);
        Assert.Null(usuario.UltimoAcceso);
        Assert.Equal(_reloj.Ahora, usuario.CreadoEn);
        Assert.Equal("hash:" + Contrasena, usuario.HashContrasena);
        Assert.Equal([CodigoRol.Farmaceutico], usuario.Roles);
    }

    [Theory]
    [InlineData("  FARM01 ", "farm01")]
    [InlineData("Juan.Perez_2-a", "juan.perez_2-a")]
    public void Crear_normaliza_el_nombre_de_usuario(string ingresado, string esperado)
    {
        Assert.Equal(esperado, CrearUsuario(ingresado).NombreUsuario);
        Assert.Equal(esperado, Usuario.NormalizarNombreUsuario(ingresado));
    }

    [Theory]
    [InlineData("ab")]                       // menos de 3
    [InlineData("usuario con espacio")]       // carácter no permitido
    [InlineData("niño")]                      // solo letras sin tilde ni ñ
    [InlineData("a@b.c")]
    public void Crear_rechaza_nombres_de_usuario_invalidos(string nombreUsuario)
    {
        Assert.Throws<ReglaNegocioError>(() => CrearUsuario(nombreUsuario));
    }

    [Fact]
    public void Crear_rechaza_un_nombre_de_usuario_de_mas_de_50_caracteres()
    {
        Assert.Throws<ReglaNegocioError>(() => CrearUsuario(new string('a', 51)));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("   ")]
    public void Crear_rechaza_un_nombre_completo_invalido(string nombreCompleto)
    {
        Assert.Throws<ReglaNegocioError>(() => Usuario.Crear("farm01", nombreCompleto,
            ContrasenaPropuesta.Crear(Contrasena), [CodigoRol.Farmaceutico], _hasher, _reloj));
    }

    [Fact]
    public void Crear_exige_al_menos_un_rol()
    {
        Assert.Throws<ReglaNegocioError>(() => Usuario.Crear("farm01", "Usuario Ficticio",
            ContrasenaPropuesta.Crear(Contrasena), [], _hasher, _reloj));
    }

    [Fact]
    public void Crear_ignora_roles_repetidos()
    {
        var usuario = CrearUsuario("med01", CodigoRol.Medico, CodigoRol.Medico, CodigoRol.Epidemiologo);

        Assert.Equal(2, usuario.Roles.Count);
    }

    // ---------- IntentarAutenticar ----------

    [Fact]
    public void Autenticar_con_la_contrasena_correcta_reinicia_el_contador_y_actualiza_el_ultimo_acceso()
    {
        var usuario = CrearUsuario();
        usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);

        var resultado = usuario.IntentarAutenticar(Contrasena, _hasher, _reloj);

        Assert.True(resultado.Exitoso);
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Equal(_reloj.Ahora, usuario.UltimoAcceso);
    }

    [Fact]
    public void Autenticar_con_contrasena_incorrecta_suma_un_intento()
    {
        var usuario = CrearUsuario();

        var resultado = usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);

        Assert.False(resultado.Exitoso);
        Assert.Equal(MotivoRechazo.ContrasenaIncorrecta, resultado.Motivo);
        Assert.Equal(1, usuario.IntentosFallidos);
    }

    [Fact]
    public void El_quinto_fallo_consecutivo_bloquea_la_cuenta_15_minutos()
    {
        var usuario = CrearUsuario();
        for (var i = 0; i < 4; i++)
            Assert.False(usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj).CuentaBloqueadaAhora);

        var quinto = usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);

        Assert.True(quinto.CuentaBloqueadaAhora);
        Assert.Equal(_reloj.Ahora.AddMinutes(15), usuario.BloqueadoHasta);
    }

    [Fact]
    public void Una_cuenta_bloqueada_rechaza_aunque_la_contrasena_sea_correcta()
    {
        var usuario = CrearUsuario();
        for (var i = 0; i < 5; i++) usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);
        _reloj.Avanzar(TimeSpan.FromMinutes(14));

        var resultado = usuario.IntentarAutenticar(Contrasena, _hasher, _reloj);

        Assert.False(resultado.Exitoso);
        Assert.Equal(MotivoRechazo.Bloqueado, resultado.Motivo);
    }

    [Fact]
    public void La_cuenta_se_desbloquea_sola_despues_de_15_minutos()
    {
        var usuario = CrearUsuario();
        for (var i = 0; i < 5; i++) usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);
        _reloj.Avanzar(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        var resultado = usuario.IntentarAutenticar(Contrasena, _hasher, _reloj);

        Assert.True(resultado.Exitoso);
        Assert.Null(usuario.BloqueadoHasta);
    }

    [Fact]
    public void Un_inicio_exitoso_corta_la_secuencia_de_fallos()
    {
        var usuario = CrearUsuario();
        for (var i = 0; i < 4; i++) usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);
        usuario.IntentarAutenticar(Contrasena, _hasher, _reloj);

        var siguiente = usuario.IntentarAutenticar("incorrecta1", _hasher, _reloj);

        Assert.False(siguiente.CuentaBloqueadaAhora);
        Assert.Equal(1, usuario.IntentosFallidos);
    }

    [Fact]
    public void Si_el_formato_del_hash_cambio_se_vuelve_a_calcular()
    {
        var usuario = CrearUsuario();
        _hasher.PedirRecalculo = true;

        var resultado = usuario.IntentarAutenticar(Contrasena, _hasher, _reloj);

        Assert.True(resultado.Exitoso);
        Assert.Equal("hash:" + Contrasena, usuario.HashContrasena);
    }
}
