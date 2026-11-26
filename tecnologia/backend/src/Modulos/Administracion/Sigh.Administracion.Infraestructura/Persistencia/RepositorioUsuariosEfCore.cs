using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>Usuarios en PostgreSQL (esquema administracion). No hay eliminación (FR-009).</summary>
internal sealed class RepositorioUsuariosEfCore(AdministracionDbContext contexto) : IRepositorioUsuarios
{
    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        contexto.Usuarios.Include(u => u.RolesAsignados).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuarioNormalizado, CancellationToken ct = default) =>
        contexto.Usuarios.Include(u => u.RolesAsignados)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuarioNormalizado, ct);

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct = default) => contexto.Usuarios.AnyAsync(ct);

    public async Task AgregarAsync(Usuario usuario, CancellationToken ct = default)
    {
        contexto.Usuarios.Add(usuario);
        await GuardarAsync(ct);
    }

    public Task ActualizarAsync(Usuario usuario, CancellationToken ct = default)
    {
        if (contexto.Entry(usuario).State == EntityState.Detached)
            contexto.Usuarios.Update(usuario);
        return GuardarAsync(ct);
    }

    private async Task GuardarAsync(CancellationToken ct)
    {
        try
        {
            await contexto.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictoError("El nombre de usuario ya existe.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoError("El usuario fue modificado por otra operación. Vuelva a intentarlo.");
        }
    }
}
