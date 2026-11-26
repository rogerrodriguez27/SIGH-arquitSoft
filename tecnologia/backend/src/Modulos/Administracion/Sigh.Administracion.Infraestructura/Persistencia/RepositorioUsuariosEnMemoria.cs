using System.Collections.Concurrent;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Paginacion;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>Implementación en memoria para pruebas (docs §2). Respeta la unicidad del nombre de usuario.</summary>
public sealed class RepositorioUsuariosEnMemoria : IRepositorioUsuarios
{
    private readonly ConcurrentDictionary<Guid, Usuario> _usuarios = new();

    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.Values.ToList();

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_usuarios.GetValueOrDefault(id));

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuarioNormalizado, CancellationToken ct = default) =>
        Task.FromResult(_usuarios.Values.FirstOrDefault(u => u.NombreUsuario == nombreUsuarioNormalizado));

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct = default) => Task.FromResult(!_usuarios.IsEmpty);

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct = default) =>
        Task.FromResult(_usuarios.Values.Count(u => u.EsAdministradorActivo));

    public Task<Pagina<Usuario>> ListarAsync(FiltroUsuarios filtro, int pagina, int tamano,
        CancellationToken ct = default)
    {
        IEnumerable<Usuario> consulta = _usuarios.Values;
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var texto = filtro.Busqueda.Trim();
            consulta = consulta.Where(u => u.NombreUsuario.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || u.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }
        if (filtro.Rol is { } rol)
            consulta = consulta.Where(u => u.Roles.Contains(rol));
        if (filtro.Estado is { } estado)
            consulta = consulta.Where(u => u.Estado == estado);

        var lista = consulta.OrderBy(u => u.NombreCompleto).ThenBy(u => u.NombreUsuario).ToList();
        var elementos = lista.Skip((pagina - 1) * tamano).Take(tamano).ToList();
        return Task.FromResult(new Pagina<Usuario>(elementos, pagina, tamano, lista.Count));
    }

    public Task AgregarAsync(Usuario usuario, CancellationToken ct = default)
    {
        lock (_usuarios)
        {
            if (_usuarios.Values.Any(u => u.NombreUsuario == usuario.NombreUsuario))
                throw new ConflictoError("El nombre de usuario ya existe.");
            _usuarios[usuario.Id] = usuario;
        }
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Usuario usuario, CancellationToken ct = default)
    {
        _usuarios[usuario.Id] = usuario;
        return Task.CompletedTask;
    }
}
