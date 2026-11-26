namespace Sigh.Administracion.Dominio.Permisos;

/// <summary>
/// Permisos fijos del sistema (data-model.md, CodigoPermiso). El código también es el nombre de la
/// política de autorización en la API.
/// </summary>
public static class CodigoPermiso
{
    public const string GestionarUsuarios = "usuarios.gestionar";
    public const string ConsultarBitacora = "bitacora.consultar";
    public const string RegistrarPacientes = "pacientes.registrar";
    public const string RegistrarTriaje = "triaje.registrar";
    public const string ConsultarHce = "hce.consultar";
    public const string RegistrarAtencion = "hce.registrar_atencion";
    public const string RecetasExamenes = "hce.recetas_examenes";
    public const string DispensarRecetas = "farmacia.dispensar";
    public const string GestionarInventario = "farmacia.inventario";
    public const string ResultadosLaboratorio = "laboratorio.resultados";
    public const string ReportesEpidemiologicos = "epidemiologia.reportes";
    public const string DashboardsGestion = "bi.dashboards";
    public const string RecibirNotificaciones = "notificaciones.recibir";

    /// <summary>Código y descripción de cada permiso, en el orden de la matriz de la spec.</summary>
    public static IReadOnlyList<(string Codigo, string Descripcion)> Todos { get; } =
    [
        (GestionarUsuarios, "Gestionar usuarios, roles y permisos"),
        (ConsultarBitacora, "Consultar la bitácora de accesos"),
        (RegistrarPacientes, "Registrar y buscar pacientes, registrar el ingreso"),
        (RegistrarTriaje, "Registrar el triaje"),
        (ConsultarHce, "Consultar la historia clínica completa"),
        (RegistrarAtencion, "Registrar y firmar una atención o una nota de corrección"),
        (RecetasExamenes, "Emitir recetas y solicitar exámenes"),
        (DispensarRecetas, "Consultar las recetas pendientes y dispensar"),
        (GestionarInventario, "Gestionar el inventario de medicamentos"),
        (ResultadosLaboratorio, "Consultar las órdenes y registrar los resultados de laboratorio"),
        (ReportesEpidemiologicos, "Consultar reportes y revisar alertas epidemiológicas"),
        (DashboardsGestion, "Consultar los dashboards de gestión"),
        (RecibirNotificaciones, "Recibir notificaciones y gestionar la suscripción"),
    ];
}
