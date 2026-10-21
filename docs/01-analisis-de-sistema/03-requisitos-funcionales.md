# Requisitos funcionales

## Requisitos funcionales

| ID | Módulo | Requisito funcional |
|---|---|---|
| RF01 | Admisión y Triaje | El sistema debe permitir registrar pacientes validando su DNI mediante el servicio de identidad RENIEC (simulado en la prueba de concepto). |
| RF02 | Admisión y Triaje | El sistema debe permitir registrar pacientes de forma manual cuando no cuenten con DNI (extranjeros o pacientes no identificados) o cuando el servicio de RENIEC no esté disponible. |
| RF03 | Admisión y Triaje | El sistema debe permitir buscar pacientes por DNI, nombres y apellidos o número de historia clínica. |
| RF04 | Admisión y Triaje | El sistema debe permitir registrar el ingreso de un paciente a una atención (consulta externa o emergencia). |
| RF05 | Admisión y Triaje | El sistema debe permitir registrar los signos vitales y el motivo de consulta del paciente en el triaje. |
| RF06 | Admisión y Triaje | El sistema debe permitir clasificar el nivel de urgencia del paciente (prioridad I a IV) y ordenar la cola de atención según dicha prioridad. |
| RF07 | HCE | El sistema debe permitir consultar la historia clínica completa del paciente en orden cronológico, incluyendo antecedentes, atenciones, diagnósticos, recetas y resultados de laboratorio. |
| RF08 | HCE | El sistema debe permitir registrar en cada atención la evolución, el diagnóstico codificado con CIE-10 y el tratamiento indicado. |
| RF09 | HCE | El sistema debe impedir la modificación o eliminación de registros clínicos ya firmados; las correcciones deben registrarse como una nota adicional que conserve el registro original. |
| RF10 | HCE | El sistema debe permitir emitir recetas electrónicas asociadas a una atención. |
| RF11 | HCE | El sistema debe permitir solicitar exámenes de laboratorio asociados a una atención. |
| RF12 | Farmacia y Laboratorio | El sistema debe permitir registrar ingresos de medicamentos (lote y fecha de vencimiento) y consultar su stock disponible. |
| RF13 | Farmacia y Laboratorio | El sistema debe notificar los medicamentos con stock por debajo del mínimo o próximos a vencer. |
| RF14 | Farmacia y Laboratorio | El sistema debe permitir consultar las recetas pendientes y registrar su dispensación, descontando automáticamente el stock. |
| RF15 | Farmacia y Laboratorio | El sistema debe permitir consultar las órdenes de laboratorio pendientes y registrar sus resultados, vinculándolos a la historia clínica del paciente. |
| RF16 | Reportes Epidemiológicos | El sistema debe permitir generar reportes de casos agrupados por diagnóstico, zona geográfica y periodo, y exportarlos. |
| RF17 | Explotación OLAP / BI | El sistema debe ejecutar periódicamente un proceso ETL que cargue los datos de la base transaccional al datamart analítico. |
| RF18 | Alerta Epidemiológica Temprana | El sistema debe analizar periódicamente el datamart para detectar aumentos inusuales de casos de un diagnóstico en una zona o periodo, comparándolos con su comportamiento histórico. |
| RF19 | Alerta Epidemiológica Temprana | El sistema debe generar y notificar alertas al epidemiólogo, y permitirle registrar su revisión y estado (pendiente, en revisión, descartada o confirmada). |
| RF20 | Explotación OLAP / BI | El sistema debe permitir consultar dashboards con indicadores de atención, diagnósticos y uso de recursos, con navegación drill-down y roll-up. |
| RF21 | Administración | El sistema debe autenticar a los usuarios mediante credenciales y emitir un token de sesión (JWT). |
| RF22 | Administración | El sistema debe permitir gestionar usuarios, roles y permisos, restringiendo el acceso a cada funcionalidad según el rol del usuario. |
| RF23 | Administración | El sistema debe permitir gestionar los catálogos de diagnósticos (CIE-10), medicamentos y exámenes de laboratorio. |
| RF24 | Administración | El sistema debe registrar cada acceso y modificación a una historia clínica (usuario, fecha, hora y acción) y permitir consultar dicho registro. |

---

## Relación entre historias de usuario y requisitos funcionales

| Historia de usuario | Requisitos funcionales relacionados |
|---|---|
| HU01 Registrar paciente con validación de DNI | RF01, RF02 |
| HU02 Buscar paciente y registrar ingreso | RF03, RF04 |
| HU03 Registrar triaje y clasificar urgencia | RF05, RF06 |
| HU04 Consultar historia clínica | RF07, RF15 |
| HU05 Registrar atención médica | RF08, RF09 |
| HU06 Emitir receta electrónica | RF10 |
| HU07 Solicitar exámenes de laboratorio | RF11 |
| HU08 Gestionar inventario de medicamentos | RF12, RF13 |
| HU09 Atender recetas | RF14 |
| HU10 Registrar resultados de laboratorio | RF15 |
| HU11 Generar reportes epidemiológicos | RF16, RF17 |
| HU12 Recibir alertas epidemiológicas | RF17, RF18, RF19 |
| HU13 Consultar dashboards de gestión | RF17, RF20 |
| HU14 Gestionar usuarios, roles y permisos | RF21, RF22 |
| HU15 Gestionar catálogos | RF23 |
| HU16 Consultar registro de accesos | RF24 |