using Sigh.Compartido.Errores;

namespace Sigh.Administracion.Dominio.Usuarios;

/// <summary>
/// Contraseña en texto plano que ya cumple la política (FR-005): 8 a 128 caracteres, con letras y
/// números. Solo vive en memoria mientras se calcula su hash; nunca se guarda ni se registra.
/// </summary>
public sealed class ContrasenaPropuesta
{
    public const int LongitudMinima = 8;
    public const int LongitudMaxima = 128;

    public string Valor { get; }

    private ContrasenaPropuesta(string valor) => Valor = valor;

    public static ContrasenaPropuesta Crear(string texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length < LongitudMinima || texto.Length > LongitudMaxima
            || !texto.Any(char.IsLetter) || !texto.Any(char.IsDigit))
        {
            throw new ReglaNegocioError(
                $"La contraseña debe tener entre {LongitudMinima} y {LongitudMaxima} caracteres, con letras y números.");
        }

        return new ContrasenaPropuesta(texto);
    }

    public override string ToString() => "********";
}
