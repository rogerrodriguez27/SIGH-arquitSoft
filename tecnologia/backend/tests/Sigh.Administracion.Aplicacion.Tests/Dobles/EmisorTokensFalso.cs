using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Aplicacion.Tests.Dobles;

/// <summary>Emisor de prueba: guarda los permisos recibidos y devuelve un token legible.</summary>
public sealed class EmisorTokensFalso(RelojFijo reloj) : IEmisorTokens
{
    public IReadOnlyCollection<string> UltimosPermisos { get; private set; } = [];

    public TokenEmitido Emitir(Usuario usuario, IReadOnlyCollection<string> permisos)
    {
        UltimosPermisos = permisos;
        return new TokenEmitido($"token-{usuario.Id}-{usuario.CambioContrasenaPendiente}", reloj.Ahora.AddMinutes(30));
    }
}
