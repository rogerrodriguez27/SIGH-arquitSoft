using System.Reflection;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Dominio.Tests.Arquitectura;

/// <summary>
/// Regla de dependencia (principio I, docs §2): el dominio solo referencia a la biblioteca base de .NET
/// y a Sigh.Compartido; Sigh.Compartido solo agrega Npgsql para la conexión compartida (research D3).
/// </summary>
public sealed class ReglaDependenciaTests
{
    private static IEnumerable<string> Referencias(Assembly ensamblado) =>
        ensamblado.GetReferencedAssemblies().Select(a => a.Name!);

    private static bool EsBibliotecaBase(string nombre) =>
        nombre.StartsWith("System", StringComparison.Ordinal) || nombre is "netstandard" or "mscorlib";

    [Fact]
    public void El_dominio_solo_referencia_la_biblioteca_base_y_Sigh_Compartido()
    {
        var ajenas = Referencias(typeof(CodigoRol).Assembly)
            .Where(n => !EsBibliotecaBase(n) && n != "Sigh.Compartido");

        Assert.Empty(ajenas);
    }

    [Fact]
    public void Sigh_Compartido_no_referencia_EF_Core_ni_ASP_NET_Core()
    {
        var ajenas = Referencias(typeof(IReloj).Assembly)
            .Where(n => !EsBibliotecaBase(n) && n != "Npgsql");

        Assert.Empty(ajenas);
    }
}
