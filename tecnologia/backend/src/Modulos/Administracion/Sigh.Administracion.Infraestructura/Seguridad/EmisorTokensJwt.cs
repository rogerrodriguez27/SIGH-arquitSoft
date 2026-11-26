using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Infraestructura.Seguridad;

/// <summary>
/// Emite el JWT HS256 de 30 minutos con los claims sub, name, role, perm y, cuando corresponde,
/// cambio_contrasena_pendiente (research D4). Los permisos se calculan con la matriz fija.
/// </summary>
internal sealed class EmisorTokensJwt(IOptions<OpcionesJwt> opciones, IReloj reloj) : IEmisorTokens
{
    private readonly JsonWebTokenHandler _manejador = new();

    public TokenEmitido Emitir(Usuario usuario, IReadOnlyCollection<string> permisos)
    {
        var jwt = opciones.Value;
        var ahora = reloj.Ahora;
        var expira = ahora.AddMinutes(jwt.DuracionMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, usuario.NombreUsuario),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(usuario.Roles.Select(r => new Claim("role", r.ToString())));
        claims.AddRange(permisos.Select(p => new Claim("perm", p)));
        if (usuario.CambioContrasenaPendiente)
            claims.Add(new Claim("cambio_contrasena_pendiente", "true"));

        var token = _manejador.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emisor,
            Audience = jwt.Audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(jwt.ClaveEnBytes()), SecurityAlgorithms.HmacSha256),
        });

        return new TokenEmitido(token, expira);
    }
}
