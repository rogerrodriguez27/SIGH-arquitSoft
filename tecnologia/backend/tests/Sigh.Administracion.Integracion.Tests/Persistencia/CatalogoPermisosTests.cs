using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Integracion.Tests.Soporte;

namespace Sigh.Administracion.Integracion.Tests.Persistencia;

/// <summary>
/// Las tablas de catálogo roles, permisos y rol_permisos (para informes SQL) coinciden con la matriz
/// fija del código (data-model.md §2).
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Category", "Integracion")]
public sealed class CatalogoPermisosTests(PostgresFixture bd)
{
    private async Task<List<(string, string)>> LeerParesAsync(string sql)
    {
        var pares = new List<(string, string)>();
        await using var comando = bd.FuenteDatos.CreateCommand(sql);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            pares.Add((lector.GetString(0), lector.GetString(1)));
        return pares;
    }

    [Fact]
    public async Task La_tabla_roles_tiene_los_8_roles_fijos()
    {
        var roles = await LeerParesAsync("SELECT codigo, nombre FROM administracion.roles ORDER BY codigo");

        Assert.Equal(Enum.GetNames<CodigoRol>().Order(), roles.Select(r => r.Item1));
        Assert.All(roles, r => Assert.Equal(CatalogoRoles.Nombre(Enum.Parse<CodigoRol>(r.Item1)), r.Item2));
    }

    [Fact]
    public async Task La_tabla_permisos_tiene_los_13_permisos_fijos()
    {
        var permisos = await LeerParesAsync("SELECT codigo, descripcion FROM administracion.permisos ORDER BY codigo");

        Assert.Equal(CodigoPermiso.Todos.OrderBy(p => p.Codigo, StringComparer.Ordinal).Select(p => (p.Codigo, p.Descripcion)),
            permisos.OrderBy(p => p.Item1, StringComparer.Ordinal));
    }

    [Fact]
    public async Task La_tabla_rol_permisos_es_identica_a_MatrizPermisos()
    {
        var enBaseDeDatos = await LeerParesAsync("SELECT rol, permiso FROM administracion.rol_permisos");
        var enCodigo = MatrizPermisos.Filas().SelectMany(f => f.Roles.Select(r => (r.ToString(), f.Codigo)));

        Assert.Equal(enCodigo.Order(), enBaseDeDatos.Order());
    }
}
