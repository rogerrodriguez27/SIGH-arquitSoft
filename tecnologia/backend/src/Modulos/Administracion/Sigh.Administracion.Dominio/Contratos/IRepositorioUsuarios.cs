using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>Puerto de persistencia de usuarios. No existe operación de eliminación (FR-009).</summary>
public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca por el nombre de usuario ya normalizado.</summary>
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuarioNormalizado, CancellationToken ct = default);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct = default);

    Task AgregarAsync(Usuario usuario, CancellationToken ct = default);

    Task ActualizarAsync(Usuario usuario, CancellationToken ct = default);
}
