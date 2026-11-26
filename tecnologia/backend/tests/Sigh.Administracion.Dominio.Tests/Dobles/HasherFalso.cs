using Sigh.Administracion.Dominio.Contratos;

namespace Sigh.Administracion.Dominio.Tests.Dobles;

/// <summary>Hasher de prueba, rápido y determinista. Nunca se usa fuera de las pruebas.</summary>
public sealed class HasherFalso : IHasherContrasenas
{
    public bool PedirRecalculo { get; set; }

    public string Calcular(string contrasena) => "hash:" + contrasena;

    public ResultadoVerificacionContrasena Verificar(string hash, string contrasena) =>
        hash != "hash:" + contrasena
            ? ResultadoVerificacionContrasena.Incorrecta
            : PedirRecalculo
                ? ResultadoVerificacionContrasena.CorrectaRequiereRecalculo
                : ResultadoVerificacionContrasena.Correcta;
}
