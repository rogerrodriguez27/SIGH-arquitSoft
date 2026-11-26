# Escenarios de atributos de calidad del SIGH

## EQ-01. Rendimiento: operaciones clínicas

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Rendimiento |
| **Atributo relacionado (ID)** | AC01 |
| **Artefacto** | API SIGH (módulos Admisión y Triaje y HCE) y base de datos `sigh_oltp` |
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
| **Artefacto** | Contenedor API SIGH, con una y con dos instancias |
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
| **Artefacto** | Contenedor API SIGH y adaptador de identidad `AdaptadorApiPeru` |
| **Fuente** | Falla interna de un contenedor o del servicio externo APIPERU |
| **Estímulo** | El contenedor del backend se detiene inesperadamente, o APIPERU no responde (o agotó su límite de consultas) durante una admisión. |
| **Condición** | El sistema está desplegado con healthchecks, reinicio automático y monitoreo activo. |
| **Respuesta esperada** | El contenedor caído se reinicia automáticamente; si APIPERU no responde, el Circuit Breaker deja de llamarlo temporalmente, el sistema permite continuar la admisión con registro manual (RF02) y comunica el error de forma controlada. |
| **Medida** | **Propuesta por validar:** el servicio se recupera automáticamente en 60 segundos o menos y el 100 % de las admisiones puede completarse aunque APIPERU no esté disponible, y cada intento fallido de consulta termina en 5 segundos o menos. Meta de diseño: 99,5 % de disponibilidad mensual. |
| **Verificación** | Detener manualmente un contenedor y medir el tiempo de recuperación; simular la caída de APIPERU (o un token inválido) y comprobar el registro manual y el tiempo de espera. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-04. Seguridad: acceso no autorizado a la historia clínica

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Seguridad (confidencialidad) |
| **Atributo relacionado (ID)** | AC04 |
| **Artefacto** | Middleware de autenticación y autorización (JWT y roles) y módulo HCE |
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
| **Artefacto** | Entidad `Atencion` y endpoints del módulo HCE |
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
| **Artefacto** | Base `sigh_oltp`, datamart `sigh_olap`, ETL, alerta epidemiológica y Metabase |
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
| **Artefacto** | Base `sigh_oltp` y backups en S3 |
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
| **Artefacto** | Prometheus, Grafana y logs centralizados |
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
| **Artefacto** | Capa de infraestructura (adaptador de identidad) y módulo de alerta epidemiológica |
| **Fuente** | Desarrollador del sistema |
| **Estímulo** | Se cambia el proveedor de identidad (por ejemplo, de APIPERU a otro servicio o al adaptador simulado de pruebas), o se desactiva el módulo de alerta epidemiológica. |
| **Condición** | El sistema está organizado en módulos con Clean Architecture y dispone de pruebas para las funciones afectadas. |
| **Respuesta esperada** | El cambio se concentra en el adaptador o módulo responsable, sin modificar los casos de uso ni afectar a los demás módulos. |
| **Medida** | El cambio del proveedor de identidad modifica solo la capa de infraestructura y la configuración, y al desactivar el módulo de alertas el 100 % de las pruebas de los módulos clínicos se aprueba. |
| **Verificación** | Revisar los archivos modificados, ejecutar las pruebas y revisar las dependencias entre módulos. |
| **Estado** | Pendiente de definir las reglas de dependencias y las pruebas obligatorias. |

## EQ-10. Disponibilidad: notificaciones push sin afectar la operación clínica

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Disponibilidad |
| **Atributo relacionado (ID)** | AC03 |
| **Artefacto** | Módulo de Notificaciones, cola de envío en segundo plano y servicio Web Push del navegador |
| **Fuente** | Personal de laboratorio, médico, farmacia o el proceso de alerta epidemiológica |
| **Estímulo** | Ocurre un evento que debe notificarse (por ejemplo, se registra un resultado de laboratorio), con el servicio Web Push disponible o caído. |
| **Condición** | Los usuarios destinatarios tienen su navegador suscrito a las notificaciones; el sistema funciona con carga normal. |
| **Respuesta esperada** | El registro clínico se completa sin esperar el envío; la notificación se guarda en la bandeja interna y se envía en segundo plano, sin datos clínicos. Si el servicio push falla, la operación clínica no se ve afectada y la notificación queda disponible en la bandeja. |
| **Medida** | **Propuesta por validar:** el 95 % de las notificaciones se entrega al servicio Web Push en 10 segundos o menos desde el evento; el 100 % queda registrado en la bandeja interna, y el 0 % de las operaciones clínicas falla o se demora por una falla del servicio push. |
| **Verificación** | Registrar eventos de prueba y medir el tiempo hasta el envío; desactivar el servicio push y comprobar que el registro clínico continúa y que las notificaciones aparecen en la bandeja; revisar que los mensajes no contengan datos clínicos. |
| **Estado** | Pendiente de validar en la prueba de concepto. |

## EQ-11. Seguridad: secretos fuera del repositorio

| Elemento | Descripción |
|---|---|
| **Atributo de calidad** | Seguridad (confidencialidad de credenciales) |
| **Atributo relacionado (ID)** | AC04 |
| **Artefacto** | Repositorio de GitHub, archivos `.gitignore` y `.env.example`, y configuración de la API SIGH |
| **Fuente** | Desarrollador del sistema |
| **Estímulo** | Se agrega o modifica una credencial (token de APIPERU, claves VAPID, clave JWT o contraseña de base de datos) y se hace commit. |
| **Condición** | El repositorio es público y se trabaja desde una o más computadoras. |
| **Respuesta esperada** | La credencial queda solo en el archivo `.env` local o en el servidor; Git la ignora y el repositorio solo contiene la plantilla `.env.example` con los valores vacíos. |
| **Medida** | 0 credenciales en el repositorio y en su historial de commits. |
| **Verificación** | Ejecutar `git check-ignore -v .env` antes de cada commit, revisar `git status` y buscar credenciales en el historial (por ejemplo, con el escaneo de secretos de GitHub o una herramienta como gitleaks). |
| **Estado** | Aplicado: `.gitignore` y `.env.example` incluidos en el repositorio. |

## Resumen de escenarios

| Código | Atributo de calidad | Qué se busca comprobar |
|---|---|---|
| EQ-01 | Rendimiento | Que las operaciones clínicas respondan dentro del tiempo establecido. |
| EQ-02 | Escalabilidad | Que el sistema soporte un aumento de usuarios y mejore al agregar instancias. |
| EQ-03 | Disponibilidad | Que el sistema se recupere de fallas y la admisión continúe aunque APIPERU no responda. |
| EQ-04 | Seguridad (confidencialidad) | Que la historia clínica no pueda consultarse ni modificarse sin autorización. |
| EQ-05 | Seguridad (integridad) | Que los registros clínicos firmados no puedan alterarse. |
| EQ-06 | Aislamiento de cargas | Que el análisis y los reportes no afecten la operación clínica. |
| EQ-07 | Recuperabilidad | Que la información pueda restaurarse dentro de los objetivos de RPO y RTO. |
| EQ-08 | Observabilidad | Que las fallas se detecten y rastreen oportunamente. |
| EQ-09 | Mantenibilidad | Que los cambios de proveedor o de módulo se realicen sin afectar a los demás. |
| EQ-10 | Disponibilidad | Que las notificaciones lleguen a tiempo y que su falla no afecte la operación clínica. |
| EQ-11 | Seguridad (credenciales) | Que ninguna credencial quede expuesta en el repositorio. |
