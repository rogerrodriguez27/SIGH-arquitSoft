namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>Puerto para calcular y verificar hashes irreversibles de contraseñas (FR-005, research D2).</summary>
public interface IHasherContrasenas
{
    string Calcular(string contrasena);

    ResultadoVerificacionContrasena Verificar(string hash, string contrasena);
}

public enum ResultadoVerificacionContrasena
{
    Incorrecta,
    Correcta,

    /// <summary>Es correcta, pero el hash usa un formato antiguo y debe volver a calcularse.</summary>
    CorrectaRequiereRecalculo,
}
