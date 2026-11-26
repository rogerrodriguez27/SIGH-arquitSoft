using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.AdministradorInicial;

/// <summary>Datos del administrador inicial, leídos de AdminInicial__* (research D7).</summary>
public sealed record DatosAdministradorInicial(string? NombreUsuario, string? NombreCompleto, string? Contrasena)
{
    public override string ToString() => "DatosAdministradorInicial { ******** }";
}

public enum ResultadoAdministradorInicial
{
    Creado,
    YaExistenUsuarios,
    FaltaConfiguracion,
    ConfiguracionInvalida,
}

/// <summary>
/// Solo en el primer arranque (sin ningún usuario) crea un Administrador con el cambio de contraseña
/// obligatorio (FR-016). Nunca hay credenciales en el código (principio VI).
/// </summary>
public sealed class CrearAdministradorInicialCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IHasherContrasenas hasher,
    IReloj reloj)
{
    public async Task<ResultadoAdministradorInicial> EjecutarAsync(DatosAdministradorInicial datos,
        CancellationToken ct = default)
    {
        if (await usuarios.ExisteAlgunoAsync(ct))
            return ResultadoAdministradorInicial.YaExistenUsuarios;

        if (string.IsNullOrWhiteSpace(datos.NombreUsuario) || string.IsNullOrWhiteSpace(datos.NombreCompleto)
            || string.IsNullOrEmpty(datos.Contrasena))
        {
            return ResultadoAdministradorInicial.FaltaConfiguracion;
        }

        Usuario administrador;
        try
        {
            administrador = Usuario.Crear(datos.NombreUsuario, datos.NombreCompleto,
                ContrasenaPropuesta.Crear(datos.Contrasena), [CodigoRol.Administrador], hasher, reloj);
        }
        catch (ReglaNegocioError)
        {
            return ResultadoAdministradorInicial.ConfiguracionInvalida;
        }

        await usuarios.AgregarAsync(administrador, ct);
        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.UsuarioCreado, reloj,
            actorId: null, objetivoId: administrador.Id, detalle: "administrador inicial; roles: Administrador"), ct);
        return ResultadoAdministradorInicial.Creado;
    }
}
