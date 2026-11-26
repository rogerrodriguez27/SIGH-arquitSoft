using Sigh.Administracion.Dominio.Contratos;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.ConsultarUsuario;

/// <summary>Consulta un usuario por su identificador (FR-008).</summary>
public sealed class ConsultarUsuarioCasoUso(IRepositorioUsuarios usuarios, IReloj reloj)
{
    public async Task<UsuarioDto> EjecutarAsync(Guid usuarioId, CancellationToken ct = default) =>
        UsuarioDto.Desde(await usuarios.ObtenerExistenteAsync(usuarioId, ct), reloj.Ahora);
}
