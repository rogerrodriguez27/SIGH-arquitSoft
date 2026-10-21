# Escenarios de atributos de calidad del SIGH

## EQ-01. Rendimiento: operaciones clínicas

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Rendimiento |
| **Atributo relacionado (ID)** | AC01 |
| **Fuente** | Médico, enfermero(a) de triaje o personal de admisión |
| **Estímulo** | El usuario busca un paciente, consulta su historia clínica o registra una atención, un triaje o una receta. |
| **Condición** | El sistema funciona normalmente en el entorno de la prueba de concepto y se utilizan datos ficticios representativos. |
| **Respuesta esperada** | El sistema muestra o registra la información solicitada sin errores inesperados. |
| **Medida** | **Propuesta por validar:** el 95 % de las operaciones clínicas responde en 2 segundos o menos, con menos del 1 % de errores técnicos, durante una prueba con 100 usuarios simultáneos. |
| **Verificación** | Ejecutar pruebas de carga y registrar los tiempos de respuesta, los errores y la cantidad de usuarios simultáneos. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-02. Escalabilidad: aumento de usuarios

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Escalabilidad |
| **Atributo relacionado (ID)** | AC02 |
| **Fuente** | Incremento de solicitudes del personal del hospital |
| **Estímulo** | Aumenta progresivamente la cantidad de usuarios que utilizan el sistema al mismo tiempo. |
| **Condición** | Se realiza una prueba con datos, configuración y solicitudes documentados, primero con una instancia del backend y luego con dos. |
| **Respuesta esperada** | El sistema soporta el aumento de usuarios sin superar los límites acordados, y al agregar una instancia mejora su capacidad sin cambios en el código. |
| **Medida** | **Propuesta por validar:** soportar hasta 200 usuarios simultáneos con el 95 % de las respuestas por debajo de 3 segundos y menos del 1 % de errores técnicos. El diseño se documenta como capaz de escalar a más de 1000 sesiones. |
| **Verificación** | Aumentar progresivamente la carga y comparar los tiempos de respuesta, los errores y el uso de recursos con una y dos instancias. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-03. Disponibilidad: falla de un servicio

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Disponibilidad |
| **Atributo relacionado (ID)** | AC03 |
| **Fuente** | Falla interna de un contenedor o del servicio externo de RENIEC |
| **Estímulo** | El contenedor del backend se detiene inesperadamente, o el servicio de RENIEC no responde durante una admisión. |
| **Condición** | El sistema está desplegado con healthchecks, reinicio automático y monitoreo activo. |
| **Respuesta esperada** | El contenedor caído se reinicia automáticamente; si RENIEC no responde, el sistema permite continuar la admisión con registro manual (RF02) y comunica el error de forma controlada. |
| **Medida** | **Propuesta por validar:** el servicio se recupera automáticamente en 60 segundos o menos y el 100 % de las admisiones puede completarse aunque RENIEC no esté disponible. Meta de diseño: 99,5 % de disponibilidad mensual. |
| **Verificación** | Detener manualmente un contenedor y medir el tiempo de recuperación; simular la caída del servicio de RENIEC y comprobar el registro manual. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-04. Seguridad: acceso no autorizado a la historia clínica

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Seguridad (confidencialidad) |
| **Atributo relacionado (ID)** | AC04 |
| **Fuente** | Usuario sin sesión, con credenciales inválidas o con un rol sin permisos suficientes (por ejemplo, farmacéutico intentando ver la evolución médica) |
| **Estímulo** | El usuario intenta consultar o modificar una historia clínica o ejecutar una función no permitida para su rol. |
| **Condición** | Se realizan pruebas sobre las funciones protegidas con usuarios de cada rol. |
| **Respuesta esperada** | El sistema rechaza la operación, no expone información clínica y registra el intento en la bitácora de auditoría. |
| **Medida** | Rechazar el 100 % de los intentos no autorizados incluidos en las pruebas y registrar el 100 % de los accesos a historias clínicas en la bitácora. |
| **Verificación** | Ejecutar pruebas de acceso autorizado y no autorizado por rol, revisar las respuestas y consultar la bitácora de auditoría. |
| **Estado** | Pendiente de definir la matriz de permisos por rol. |

## EQ-05. Seguridad: integridad de los registros clínicos

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Seguridad (integridad) |
| **Atributo relacionado (ID)** | AC04 |
| **Fuente** | Médico u otro usuario con acceso a la historia clínica |
| **Estímulo** | El usuario intenta modificar o eliminar un registro clínico ya firmado. |
| **Condición** | El registro clínico fue firmado previamente. |
| **Respuesta esperada** | El sistema impide la modificación o eliminación y solo permite agregar una nota de corrección vinculada al registro original, que se conserva sin cambios. |
| **Medida** | El 100 % de los intentos de modificación o eliminación de registros firmados es rechazado, y el registro original permanece intacto. |
| **Verificación** | Ejecutar pruebas que intenten modificar y eliminar registros firmados, tanto desde la aplicación como directamente desde la API. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-06. Aislamiento de cargas: análisis sin afectar la operación clínica

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Aislamiento de cargas (OLTP / OLAP) |
| **Atributo relacionado (ID)** | AC05 |
| **Fuente** | Proceso ETL, proceso de alerta epidemiológica, epidemiólogo o directivo |
| **Estímulo** | Se ejecuta el ETL, el análisis de alertas o se consultan dashboards mientras el personal realiza operaciones clínicas. |
| **Condición** | El sistema está bajo una carga clínica normal en el entorno de la prueba de concepto. |
| **Respuesta esperada** | Las operaciones clínicas mantienen su tiempo de respuesta, ya que las cargas analíticas se ejecutan sobre el datamart. |
| **Medida** | **Propuesta por validar:** el tiempo de respuesta del percentil 95 de las operaciones clínicas no aumenta más de un 20 % mientras se ejecutan las cargas analíticas, y sigue por debajo de 2 segundos. |
| **Verificación** | Ejecutar una prueba de carga clínica con y sin cargas analíticas en paralelo y comparar los tiempos de respuesta. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-07. Recuperabilidad: restauración ante pérdida de datos

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Recuperabilidad |
| **Atributo relacionado (ID)** | AC06 |
| **Fuente** | Falla grave del servidor o pérdida de la base de datos |
| **Estímulo** | La base de datos transaccional deja de estar disponible o se pierde su información. |
| **Condición** | Existen backups automáticos en S3 (snapshots + WAL) configurados según el procedimiento documentado. |
| **Respuesta esperada** | La base de datos se restaura desde los backups siguiendo el procedimiento documentado. |
| **Medida** | **Meta de diseño:** pérdida máxima de 15 minutos de información (RPO ≤ 15 min) y restauración en 4 horas o menos (RTO ≤ 4 h). |
| **Verificación** | Realizar un simulacro de restauración en un entorno de prueba y medir la información perdida y el tiempo total de recuperación. |
| **Estado** | Pendiente de documentar el procedimiento de backup y restauración. |

## EQ-08. Observabilidad: detección de fallas

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Observabilidad |
| **Atributo relacionado (ID)** | AC07 |
| **Fuente** | Falla o degradación de un servicio |
| **Estímulo** | Un servicio deja de responder o sus tiempos de respuesta aumentan. |
| **Condición** | El monitoreo con Grafana / Prometheus y los logs centralizados están activos. |
| **Respuesta esperada** | La falla se refleja en el panel de monitoreo y puede rastrearse en los logs hasta la operación que la originó. |
| **Medida** | **Propuesta por validar:** la falla es visible en el panel de monitoreo en 2 minutos o menos. |
| **Verificación** | Provocar una falla controlada y medir el tiempo hasta que aparece en el panel; revisar los logs asociados. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-09. Mantenibilidad: cambios sin afectar otros módulos

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Mantenibilidad |
| **Atributo relacionado (ID)** | AC08 |
| **Fuente** | Desarrollador del sistema |
| **Estímulo** | Se reemplaza el servicio simulado de RENIEC por otro proveedor, o se desactiva el módulo de alerta epidemiológica. |
| **Condición** | El sistema está organizado en módulos con Clean Architecture y dispone de pruebas para las funciones afectadas. |
| **Respuesta esperada** | El cambio se concentra en el adaptador o módulo responsable, sin modificar los casos de uso ni afectar a los demás módulos. |
| **Medida** | El cambio del proveedor de RENIEC modifica solo la capa de infraestructura, y al desactivar el módulo de alertas el 100 % de las pruebas de los módulos clínicos se aprueba. |
| **Verificación** | Revisar los archivos modificados, ejecutar las pruebas y revisar las dependencias entre módulos. |
| **Estado** | Pendiente de definir las reglas de dependencias y las pruebas obligatorias. |

## Resumen de escenarios

| Código | Atributo de calidad | Qué se busca comprobar |
|---|---|---|
| EQ-01 | Rendimiento | Que las operaciones clínicas respondan dentro del tiempo establecido. |
| EQ-02 | Escalabilidad | Que el sistema soporte un aumento de usuarios y mejore al agregar instancias. |
| EQ-03 | Disponibilidad | Que el sistema se recupere de fallas y la admisión continúe aunque RENIEC no responda. |
| EQ-04 | Seguridad (confidencialidad) | Que la historia