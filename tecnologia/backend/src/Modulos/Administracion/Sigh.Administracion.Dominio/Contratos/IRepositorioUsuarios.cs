using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Paginacion;

namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>Puerto de persistencia de usuarios. No existe operación de eliminación (FR-009).</summary>
public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca por el nombre de usuario ya normalizado.</summary>
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuarioNormalizado, CancellationToken ct = default);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct = default);

    /// <summary>Administradores con estado Activo (FR-014).</summary>
    Task<int> ContarAdministradoresActivosAsync(CancellationToken ct = default);

    /// <summary>Lista ordenada por nombre completo, con búsqueda y filtros (FR-008).</summary>
    Task<Pagina<Usuario>> ListarAsync(FiltroUsuarios filtro, int pagina, int tamano, CancellationToken ct = default);

    Task AgregarAsync(Usuario usuario, CancellationToken ct = default);

    Task ActualizarAsync(Usuario usuario, CancellationToken ct = default);
}

/// <summary>Búsqueda por nombre o usuario (sin distinguir mayúsculas) y filtro por rol y estado.</summary>
public sealed record FiltroUsuarios(string? Busqueda = null, CodigoRol? Rol = null, EstadoUsuario? Estado = null);
