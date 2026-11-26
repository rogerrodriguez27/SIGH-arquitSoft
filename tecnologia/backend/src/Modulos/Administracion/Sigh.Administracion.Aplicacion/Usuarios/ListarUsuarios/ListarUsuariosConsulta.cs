using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Aplicacion.Usuarios.ListarUsuarios;

public sealed record ListarUsuariosConsulta(
    string? Busqueda = null,
    CodigoRol? Rol = null,
    EstadoUsuario? Estado = null,
    int Pagina = 1,
    int Tamano = 20);
