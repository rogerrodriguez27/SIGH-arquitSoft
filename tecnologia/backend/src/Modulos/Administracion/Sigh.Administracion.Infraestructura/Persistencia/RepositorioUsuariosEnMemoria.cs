using System.Collections.Concurrent;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

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
