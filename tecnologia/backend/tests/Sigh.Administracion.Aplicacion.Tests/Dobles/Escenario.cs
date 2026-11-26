using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura.Persistencia;

namespace Sigh.Administracion.Aplicacion.Tests.Dobles;

/// <summary>Dobles en memoria compartidos por una prueba (principio X). Usuarios ficticios (principio IX).</summary>
public sealed class Escenario
{
    public RelojFijo Reloj { get; } = RelojFijo.Nuevo();
    public HasherFalso Hasher { get; } = new();
    public RepositorioUsuariosEnMemoria Usuarios { get; } = new();
    public RepositorioEventosSeguridadEnMemoria Eventos { get; } = new();
    public EmisorTokensFalso Emisor { get; }

    public Escenario() => Emisor = new EmisorTokensFalso(Reloj);

    public async Task<Usuario> AgregarUsuarioAsync(string nombreUsuario, string contrasena, params CodigoRol[] roles)
    {
        var usuario = Usuario.Crear(nombreUsuario, "Usuario Ficticio", ContrasenaPropuesta.Crear(contrasena),
            roles, Hasher, Reloj);
        await Usuarios.AgregarAsync(usuario);
        return usuario;
    }

    public IReadOnlyList<TipoEventoSeguridad> TiposDeEvento() => Eventos.Eventos.Select(e => e.Tipo).ToList();
}
