namespace Sigh.Administracion.Dominio.Roles;

/// <summary>Nombre visible y descripción de cada rol fijo (docs/01-analisis-de-sistema/01-actores.md).</summary>
public static class CatalogoRoles
{
    public static IReadOnlyList<(CodigoRol Codigo, string Nombre, string Descripcion)> Todos { get; } =
    [
        (CodigoRol.Administrador, "Administrador",
            "Gestiona usuarios, roles y permisos, y supervisa el funcionamiento del sistema."),
        (CodigoRol.PersonalAdmision, "Personal de Admisión",
            "Registra y busca pacientes, actualiza sus datos de filiación y registra el ingreso."),
        (CodigoRol.EnfermeroTriaje, "Enfermero(a) de Triaje",
            "Registra signos vitales y motivo de consulta, y clasifica el nivel de urgencia."),
        (CodigoRol.Medico, "Médico",
            "Consulta la historia clínica, registra atenciones, emite recetas y solicita exámenes."),
        (CodigoRol.Farmaceutico, "Farmacéutico",
            "Gestiona el inventario de medicamentos y dispensa las recetas."),
        (CodigoRol.PersonalLaboratorio, "Personal de Laboratorio",
            "Recibe las órdenes de exámenes y registra sus resultados."),
        (CodigoRol.Epidemiologo, "Epidemiólogo",
            "Consulta reportes epidemiológicos, revisa alertas tempranas y explora dashboards."),
        (CodigoRol.Directivo, "Directivo",
            "Consulta dashboards e indicadores de gestión."),
    ];

    public static string Nombre(CodigoRol rol) => Todos.Single(r => r.Codigo == rol).Nombre;
}
