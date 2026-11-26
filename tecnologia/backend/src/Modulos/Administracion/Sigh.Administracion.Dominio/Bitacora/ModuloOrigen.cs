namespace Sigh.Administracion.Dominio.Bitacora;

/// <summary>Módulos que registran accesos a la HCE (data-model.md, EntradaBitacora.ModuloOrigen).</summary>
public static class ModuloOrigen
{
    public const string Hce = "Hce";
    public const string Admision = "Admision";
    public const string Farmacia = "Farmacia";
    public const string Laboratorio = "Laboratorio";

    public static IReadOnlySet<string> Todos { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Hce, Admision, Farmacia, Laboratorio };
}
