namespace Sigh.Administracion.Dominio.Bitacora;

/// <summary>
/// Lista cerrada de acciones de la bitácora (data-model.md, EntradaBitacora.Accion). Se amplía solo
/// con una nueva versión del sistema.
/// </summary>
public static class AccionBitacora
{
    public const string ConsultarHistoria = "CONSULTAR_HISTORIA";
    public const string RegistrarAtencion = "REGISTRAR_ATENCION";
    public const string AgregarNotaCorreccion = "AGREGAR_NOTA_CORRECCION";
    public const string RegistrarResultado = "REGISTRAR_RESULTADO";
    public const string DispensarReceta = "DISPENSAR_RECETA";
    public const string RegistrarIngreso = "REGISTRAR_INGRESO";
    public const string RegistrarTriaje = "REGISTRAR_TRIAJE";

    public static IReadOnlySet<string> Todas { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ConsultarHistoria, RegistrarAtencion, AgregarNotaCorreccion, RegistrarResultado,
        DispensarReceta, RegistrarIngreso, RegistrarTriaje,
    };
}
