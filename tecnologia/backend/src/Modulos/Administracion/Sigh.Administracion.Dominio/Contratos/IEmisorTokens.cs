using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>Puerto para emitir la sesión firmada del usuario (FR-001, FR-002, research D4).</summary>
public interface IEmisorTokens
{
    TokenEmitido Emitir(Usuario usuario, IReadOnlyCollection<string> permisos);
}

public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn);
