using Sigh.Administracion.Dominio.Contratos;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Paginacion;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.ListarUsuarios;

/// <summary>Lista usuarios con búsqueda por nombre o usuario y filtro por rol y estado (FR-008).</summary>
public sealed class ListarUsuariosCasoUso(IRepositorioUsuarios usuarios, IReloj reloj)
{
    public const int TamanoMaximo = 100;
    public const int LongitudMaximaBusqueda = 100;

    public async Task<Pagina<UsuarioDto>> EjecutarAsync(ListarUsuariosConsulta consulta, CancellationToken ct = default)
    {
        if (consulta.Pagina < 1 || consulta.Tamano < 1 || consulta.Tamano > TamanoMaximo)
            throw new ReglaNegocioError($"La página debe ser 1 o mayor y el tamaño entre 1 y {TamanoMaximo}.");
        if (consulta.Busqueda?.Length > LongitudMaximaBusqueda)
            throw new ReglaNegocioError($"La búsqueda no puede superar {LongitudMaximaBusqueda} caracteres.");

        var pagina = await usuarios.ListarAsync(
            new FiltroUsuarios(consulta.Busqueda, consulta.Rol, consulta.Estado), consulta.Pagina, consulta.Tamano, ct);

        var ahora = reloj.Ahora;
        return new Pagina<UsuarioDto>(pagina.Elementos.Select(u => UsuarioDto.Desde(u, ahora)).ToList(),
            pagina.NumeroPagina, pagina.Tamano, pagina.Total);
    }
}
