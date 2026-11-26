using Sigh.Administracion.Dominio.Contratos;

namespace Sigh.Administracion.Aplicacion.Tests.Dobles;

/// <summary>Hasher de prueba, rápido y determinista. Cuenta las verificaciones (research D6).</summary>
public sealed class HasherFalso : IHasherContrasenas
{
    public int Verificaciones { get; private set; }

    public string Calcular(string contrasena) => "hash:" + contrasena;

    public ResultadoVerificacionContrasena Verificar(string hash, string contrasena)
    {
        Verificaciones++;
        return hash == "hash:" + contrasena
            ? ResultadoVerificacionContrasena.Correcta
            : ResultadoVerificacionContrasena.Incorrecta;
    }
}
