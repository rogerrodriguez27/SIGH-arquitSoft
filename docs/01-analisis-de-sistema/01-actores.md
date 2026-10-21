# Actores

## Actores humanos

| Actor | ¿Qué necesita realizar? |
|---|---|
| **Personal de Admisión** | Registrar pacientes nuevos, buscar pacientes existentes, actualizar sus datos de filiación y registrar el ingreso a una atención. |
| **Enfermero(a) de Triaje** | Registrar signos vitales y motivo de consulta, y clasificar el nivel de urgencia del paciente al ingreso. |
| **Médico** | Consultar la historia clínica del paciente, registrar evoluciones, diagnósticos y tratamientos, emitir recetas y solicitar exámenes de laboratorio. |
| **Farmacéutico** | Gestionar el inventario de medicamentos, atender las recetas emitidas por el médico y registrar la dispensación. |
| **Personal de Laboratorio** | Recibir las órdenes de exámenes, registrar los resultados y vincularlos a la historia clínica del paciente. |
| **Epidemiólogo** | Consultar reportes epidemiológicos por diagnóstico, zona y periodo, revisar las alertas epidemiológicas tempranas y explorar los dashboards analíticos. |
| **Directivo / Gestor del hospital** | Consultar dashboards e indicadores de gestión (OLAP / BI) para la toma de decisiones. |
| **Administrador del sistema** | Gestionar usuarios, roles y permisos, configurar catálogos (diagnósticos, medicamentos, exámenes) y supervisar el funcionamiento del sistema. |
| **Paciente** *(actor indirecto)* | No usa el sistema directamente. Es el titular de la historia clínica y proporciona la información que registran los demás actores. |

## Actores de sistema

| Actor | ¿Qué necesita realizar? |
|---|---|
| **Programador de tareas (cron)** | Ejecutar periódicamente el proceso ETL que carga los datos al datamart y el análisis de alerta epidemiológica temprana. |
| **Proceso ETL** | Extraer los datos de la base transaccional (OLTP), transformarlos y cargarlos en el datamart analítico (OLAP). |
| **Herramienta de BI (Metabase / Superset)** | Consultar el datamart para mostrar dashboards y reportes multidimensionales. |
| **Servicio de identidad RENIEC** *(simulado)* | Validar el DNI del paciente durante la admisión y devolver sus datos básicos de identificación (nombres, apellidos, fecha de nacimiento). En la prueba de concepto se usa un servicio simulado con datos ficticios, ya que la integración real requiere un convenio con RENIEC. |