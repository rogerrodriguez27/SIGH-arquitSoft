using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Compartido.Paginacion;
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

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct = default) =>
        contexto.Usuarios.CountAsync(u => u.Estado == EstadoUsuario.Activo
            && u.RolesAsignados.Any(r => r.Rol == CodigoRol.Administrador), ct);

    public async Task<Pagina<Usuario>> ListarAsync(FiltroUsuarios filtro, int pagina, int tamano,
        CancellationToken ct = default)
    {
        var consulta = contexto.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var patron = $"%{EscaparLike(filtro.Busqueda.Trim())}%";
            consulta = consulta.Where(u => EF.Functions.ILike(u.NombreUsuario, patron, "\\")
                || EF.Functions.ILike(u.NombreCompleto, patron, "\\"));
        }
        if (filtro.Rol is { } rol)
            consulta = consulta.Where(u => u.RolesAsignados.Any(r => r.Rol == rol));
        if (filtro.Estado is { } estado)
            consulta = consulta.Where(u => u.Estado == estado);

        var total = await consulta.CountAsync(ct);
        var elementos = await consulta
            .Include(u => u.RolesAsignados)
            .OrderBy(u => u.NombreCompleto).ThenBy(u => u.NombreUsuario)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync(ct);

        return new Pagina<Usuario>(elementos, pagina, tamano, total);
    }

    private static string EscaparLike(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

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
