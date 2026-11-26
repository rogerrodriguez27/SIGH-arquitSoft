using Sigh.Administracion.Dominio.Roles;
using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Dominio.Usuarios;

/// <summary>
/// Servicio de dominio con las reglas que involucran a varios usuarios (FR-014, data-model.md):
/// el sistema nunca queda sin un administrador activo, y un administrador no puede desactivarse a sí
/// mismo ni quitarse su rol Administrador.
/// </summary>
public static class PoliticaAdministradores
{
    /// <param name="actorId">Usuario que hace la operación (del token).</param>
    /// <param name="administradoresActivos">Cantidad actual de administradores activos, incluido el objetivo.</param>
    public static void ValidarDesactivacion(Guid actorId, Usuario objetivo, int administradoresActivos)
    {
        if (objetivo.Id == actorId)
            throw new ReglaNegocioError("No puede desactivar su propia cuenta.");

        if (objetivo.EsAdministradorActivo && administradoresActivos <= 1)
            throw new ReglaNegocioError("No se puede desactivar al último administrador activo.");
    }

    public static void ValidarCambioRoles(Guid actorId, Usuario objetivo, IEnumerable<CodigoRol> nuevosRoles,
        int administradoresActivos)
    {
        var pierdeAdministrador = objetivo.Roles.Contains(CodigoRol.Administrador)
            && !nuevosRoles.Contains(CodigoRol.Administrador);
        if (!pierdeAdministrador)
            return;

        if (objetivo.Id == actorId)
            throw new ReglaNegocioError("No puede quitarse su propio rol Administrador.");

        if (objetivo.EsAdministradorActivo && administradoresActivos <= 1)
            throw new ReglaNegocioError("No se puede quitar el rol Administrador al último administrador activo.");
    }
}
