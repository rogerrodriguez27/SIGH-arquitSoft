using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Api.Tests.Soporte;

/// <summary>Emite tokens de prueba: por rol, vencidos, con firma inválida o con cambio pendiente.</summary>
public sealed class TokensPrueba(string claveFirma, string emisor, string audiencia)
{
    public string ParaRoles(params CodigoRol[] roles) => Crear(roles, DateTime.UtcNow.AddMinutes(30));

    public string ParaUsuario(Guid usuarioId, params CodigoRol[] roles) =>
        Crear(roles, DateTime.UtcNow.AddMinutes(30), usuarioId: usuarioId);

    public string ConCambioPendiente(params CodigoRol[] roles) =>
        Crear(roles, DateTime.UtcNow.AddMinutes(30), cambioPendiente: true);

    /// <summary>Venció hace 5 minutos: más que el margen de 30 s (research D4).</summary>
    public string Vencido(params CodigoRol[] roles) =>
        Crear(roles, DateTime.UtcNow.AddMinutes(-5), emitidoEn: DateTime.UtcNow.AddMinutes(-35));

    public string ConFirmaInvalida(params CodigoRol[] roles) =>
        Crear(roles, DateTime.UtcNow.AddMinutes(30), clave: Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));

    private string Crear(CodigoRol[] roles, DateTime expira, bool cambioPendiente = false,
        DateTime? emitidoEn = null, string? clave = null, Guid? usuarioId = null)
    {
        var claims = new List<Claim>
        {
            new("sub", (usuarioId ?? Guid.NewGuid()).ToString()),
            new("name", "usuario.prueba"),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r.ToString())));
        claims.AddRange(MatrizPermisos.PermisosDe(roles).Select(p => new Claim("perm", p)));
        if (cambioPendiente)
            claims.Add(new Claim("cambio_contrasena_pendiente", "true"));

        var emitido = emitidoEn ?? DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = emisor,
            Audience = audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = emitido,
            NotBefore = emitido,
            Expires = expira,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave ?? claveFirma)), SecurityAlgorithms.HmacSha256),
        });
    }
}
