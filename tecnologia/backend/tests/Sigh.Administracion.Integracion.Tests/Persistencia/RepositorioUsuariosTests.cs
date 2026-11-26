using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Administracion.Integracion.Tests.Soporte;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Integracion.Tests.Persistencia;

/// <summary>Tablas usuarios y usuario_roles (data-model.md §2) con PostgreSQL real.</summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Category", "Integracion")]
public sealed class RepositorioUsuariosTests(PostgresFixture bd)
{
    private sealed class HasherSimple : IHasherContrasenas
    {
        public string Calcular(string contrasena) => "hash:" + contrasena;

        public ResultadoVerificacionContrasena Verificar(string hash, string contrasena) =>
            hash == "hash:" + contrasena ? ResultadoVerificacionContrasena.Correcta : ResultadoVerificacionContrasena.Incorrecta;
    }

    private static Usuario NuevoUsuario(string nombre, params CodigoRol[] roles) =>
        Usuario.Crear(nombre, "Usuario Ficticio", ContrasenaPropuesta.Crear("Clave2026"),
            roles.Length == 0 ? [CodigoRol.Medico] : roles, new HasherSimple(), new RelojSistema());

    private static string NombreUnico(string prefijo) => $"{prefijo}{Guid.NewGuid():N}"[..20];

    private async Task<long> EscalarAsync(string sql)
    {
        await using var comando = bd.FuenteDatos.CreateCommand(sql);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Guarda_y_recupera_el_usuario_con_sus_roles()
    {
        var usuario = NuevoUsuario(NombreUnico("med"), CodigoRol.Medico, CodigoRol.Epidemiologo);
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(usuario);

        await using var lectura = bd.CrearContexto();
        var leido = await new RepositorioUsuariosEfCore(lectura).ObtenerPorNombreUsuarioAsync(usuario.NombreUsuario);

        Assert.NotNull(leido);
        Assert.Equal([CodigoRol.Medico, CodigoRol.Epidemiologo], leido.Roles.Order());
        Assert.Equal(EstadoUsuario.Activo, leido.Estado);
    }

    [Fact]
    public async Task El_nombre_de_usuario_es_unico()
    {
        var nombre = NombreUnico("farm");
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(NuevoUsuario(nombre));

        await using var otro = bd.CrearContexto();
        await Assert.ThrowsAsync<ConflictoError>(() => new RepositorioUsuariosEfCore(otro).AgregarAsync(NuevoUsuario(nombre)));
    }

    [Fact]
    public async Task El_estado_solo_admite_Activo_o_Desactivado()
    {
        var usuario = NuevoUsuario(NombreUnico("lab"));
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(usuario);

        await Assert.ThrowsAsync<PostgresException>(() =>
            EscalarAsync($"UPDATE administracion.usuarios SET estado = 'Eliminado' WHERE id = '{usuario.Id}'"));
    }

    [Fact]
    public async Task El_rol_solo_admite_los_8_roles_fijos()
    {
        var usuario = NuevoUsuario(NombreUnico("dir"));
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(usuario);

        await Assert.ThrowsAsync<PostgresException>(() =>
            EscalarAsync($"INSERT INTO administracion.usuario_roles (usuario_id, rol) VALUES ('{usuario.Id}', 'Superusuario')"));
    }

    [Fact]
    public async Task No_se_puede_borrar_un_usuario_con_roles()
    {
        var usuario = NuevoUsuario(NombreUnico("epi"));
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(usuario);

        await Assert.ThrowsAsync<PostgresException>(() =>
            EscalarAsync($"DELETE FROM administracion.usuarios WHERE id = '{usuario.Id}'"));
    }

    [Fact]
    public async Task Cada_actualizacion_sube_la_version_y_una_copia_vieja_genera_conflicto()
    {
        var usuario = NuevoUsuario(NombreUnico("tri"));
        await using (var contexto = bd.CrearContexto())
            await new RepositorioUsuariosEfCore(contexto).AgregarAsync(usuario);

        await using var primero = bd.CrearContexto();
        await using var segundo = bd.CrearContexto();
        var copiaA = await new RepositorioUsuariosEfCore(primero).ObtenerPorIdAsync(usuario.Id);
        var copiaB = await new RepositorioUsuariosEfCore(segundo).ObtenerPorIdAsync(usuario.Id);

        copiaA!.IntentarAutenticar("Incorrecta1", new HasherSimple(), new RelojSistema());
        await new RepositorioUsuariosEfCore(primero).ActualizarAsync(copiaA);
        Assert.Equal(1L, await EscalarAsync($"SELECT version FROM administracion.usuarios WHERE id = '{usuario.Id}'"));

        copiaB!.IntentarAutenticar("Incorrecta1", new HasherSimple(), new RelojSistema());
        await Assert.ThrowsAsync<ConflictoError>(() => new RepositorioUsuariosEfCore(segundo).ActualizarAsync(copiaB));
    }
}
