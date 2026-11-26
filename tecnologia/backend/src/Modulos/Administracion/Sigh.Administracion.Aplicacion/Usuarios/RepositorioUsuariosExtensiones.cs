using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Aplicacion.Usuarios;

internal static class RepositorioUsuariosExtensiones
{
    public static async Task<Usuario> ObtenerExistenteAsync(this IRepositorioUsuarios usuarios, Guid id,
        CancellationToken ct) =>
        await usuarios.ObtenerPorIdAsync(id, ct) ?? throw new RecursoNoEncontradoError("El usuario no existe.");
}
