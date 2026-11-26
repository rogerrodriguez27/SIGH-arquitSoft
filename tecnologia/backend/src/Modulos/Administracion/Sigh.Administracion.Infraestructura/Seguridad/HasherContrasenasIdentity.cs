using Microsoft.AspNetCore.Identity;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Infraestructura.Seguridad;

/// <summary>
/// Hash irreversible con PasswordHasher&lt;T&gt; de ASP.NET Core Identity: PBKDF2-HMAC-SHA512 con sal
/// aleatoria (FR-005, research D2).
/// </summary>
internal sealed class HasherContrasenasIdentity : IHasherContrasenas
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Calcular(string contrasena) => _hasher.HashPassword(null!, contrasena);

    public ResultadoVerificacionContrasena Verificar(string hash, string contrasena)
    {
        if (string.IsNullOrEmpty(hash))
            return ResultadoVerificacionContrasena.Incorrecta;

        return _hasher.VerifyHashedPassword(null!, hash, contrasena) switch
        {
            PasswordVerificationResult.Success => ResultadoVerificacionContrasena.Correcta,
            PasswordVerificationResult.SuccessRehashNeeded => ResultadoVerificacionContrasena.CorrectaRequiereRecalculo,
            _ => ResultadoVerificacionContrasena.Incorrecta,
        };
    }
}
