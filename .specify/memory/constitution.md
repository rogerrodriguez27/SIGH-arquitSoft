# Constitución del SIGH (Sistema Integral de Gestión Hospitalaria)

## Principios fundamentales

### I. Monolito modular con Clean Architecture

- El backend DEBE ser una única aplicación desplegable en C# / .NET (ASP.NET Core),
  organizada como monolito modular (ADR-001, RC07). No se crean microservicios.
- Cada módulo DEBE separarse en las capas Dominio, Aplicación, Infraestructura y
  Presentación (ADR-002). Las dependencias apuntan siempre hacia el Dominio.
- El Dominio NO DEBE depender de paquetes externos (EF Core, Npgsql, Redis, HttpClient,
  ASP.NET Core, librerías Web Push, etc.): solo de la biblioteca base de .NET. Las reglas de
  negocio viven en las entidades de dominio, no en controladores, repositorios ni SQL.
- La Aplicación depende solo de interfaces del Dominio, inyectadas por constructor; las
  implementaciones se registran en el `DependencyInjection.cs` de cada módulo (SOLID-D).
- Patrones de referencia obligatorios donde aplican: Repository, Adapter, Decorator (caché),
  Facade, Circuit Breaker y Observer (docs/03-diseño-de-software).

**Justificación:** una sola persona desarrolla y opera el sistema con bajo presupuesto
(RC04, RC05, DA04), y cada módulo debe poder especificarse y generarse con SDD de forma
independiente (DA08, RC02).

### II. Módulos aislados y comunicación por fachadas

- Los módulos del sistema son: Admisión y Triaje, HCE (núcleo), Farmacia, Laboratorio,
  Administración, Reportes y Alertas, y Notificaciones (transversal). Agregar, fusionar o
  eliminar un módulo requiere enmendar esta constitución.
- La HCE es el núcleo (RC01, DA01): Admisión y Triaje, Farmacia y Laboratorio vinculan su
  información a la historia clínica.
- Un módulo SOLO DEBE usar a otro a través de su fachada pública (por ejemplo,
  `IHceFachada`) o publicando eventos de dominio. NUNCA DEBE leer ni escribir las tablas,
  entidades, repositorios ni el `DbContext` de otro módulo.
- Cada módulo es dueño de sus tablas en `sigh_oltp`; las claves de otros módulos se guardan
  como identificadores, sin relaciones de navegación entre módulos.

**Justificación:** permite cambiar la estructura interna de un módulo sin afectar a los
demás y separarlos en el futuro si se requiere (EQ-09, AC08).

### III. API REST versionada, segura y auditada

- El frontend DEBE ser una SPA en React, separada del backend (ADR-003, RC10), que se
  comunica solo por HTTPS / JSON con la API REST (RC11, DA10).
- Todos los endpoints públicos DEBEN exponerse bajo `/api/v1`. Un cambio incompatible de
  contrato exige una nueva versión (`/api/v2`), no la modificación de la existente.
- Todo endpoint, salvo el de autenticación, DEBE exigir un JWT válido y autorización por rol
  (RBAC) (ADR-007, RF21, RF22). La API no guarda sesiones en el servidor (DA07).
- Todo acceso o modificación a una historia clínica DEBE registrarse en la bitácora de
  auditoría con usuario, fecha, hora y acción (RN-06, RF24), incluidos los intentos
  rechazados (EQ-04).
- TLS es obligatorio en el despliegue; CORS solo admite el origen del frontend del SIGH.
- Los errores de reglas de negocio se responden de forma controlada (`404`, `422`, `403`),
  sin exponer información clínica ni detalles internos.

**Justificación:** confidencialidad, integridad y trazabilidad de la información clínica
(DA02, RC15, RC16, EQ-04).

### IV. Separación OLTP / OLAP y procesos analíticos externos

- La operación clínica DEBE usar PostgreSQL `sigh_oltp`; el análisis DEBE usar el datamart
  PostgreSQL `sigh_olap`, físicamente separado (ADR-004, RC08, DA03).
- El ETL y la alerta epidemiológica DEBEN implementarse en Python y ejecutarse con cron,
  como contenedores fuera del monolito (ADR-004, ADR-005). El ETL es el único proceso que
  lee `sigh_oltp` para cargar `sigh_olap`.
- Reportes y Alertas, Metabase y la alerta epidemiológica NO DEBEN consultar `sigh_oltp`.
- La alerta epidemiológica avisa al monolito solo mediante un endpoint interno protegido.
- Redis se usa como caché compartida de catálogos (CIE-10, medicamentos, exámenes) y datos
  de consulta frecuente mediante el patrón Decorator (ADR-006). Redis NO es fuente de
  verdad: si falla, el sistema DEBE seguir funcionando consultando PostgreSQL.

**Justificación:** las cargas analíticas no deben degradar la operación clínica (EQ-06,
AC05) y las operaciones clínicas deben responder en menos de 2 s (EQ-01, DA05).

### V. Registros clínicos firmados inmutables (NO NEGOCIABLE)

- Una atención firmada NO DEBE modificarse ni eliminarse por ninguna vía: API, caso de uso,
  repositorio ni script (RN-04, RF09, ADR-008).
- Las correcciones DEBEN registrarse como `NotaCorreccion` vinculada al registro original,
  con autor y fecha (RN-05).
- Las reglas RN-01 a RN-06 DEBEN aplicarse en el Dominio: toda atención pertenece a una
  historia clínica existente (RN-01); solo se firma con al menos un diagnóstico CIE-10
  válido (RN-02, RC17); solo un usuario con rol Médico puede registrar una atención, y solo
  el médico que la registró puede firmarla (RN-03).
- La persistencia de registros clínicos firmados es de solo inserción.

**Justificación:** la historia clínica es un documento legal (Ley N° 30024, RC16, EQ-05).

### VI. Secretos fuera del repositorio (NO NEGOCIABLE)

- El token de APIPERU, las claves VAPID, la clave de firma JWT y las contraseñas de bases
  de datos DEBEN leerse solo desde variables de entorno (ADR-012, RC18), con la convención
  de .NET `Seccion__Clave` (por ejemplo, `ApiPeru__Token`).
- NUNCA DEBEN aparecer en el código, en `appsettings*.json`, en `docker-compose.yml`, en la
  documentación, en los logs ni en el historial de Git.
- El repositorio solo contiene `.env.example` con los nombres de las variables y valores
  vacíos; `.env` DEBE estar en `.gitignore`.
- Si una credencial llega a exponerse, se revoca y se regenera en el proveedor de inmediato.

**Justificación:** el repositorio es público (EQ-11, AC04).

### VII. Integración resiliente con APIPERU

- Los casos de uso DEBEN depender solo del contrato `IServicioIdentidad`; la llamada a
  APIPERU se encapsula en `AdaptadorApiPeru` (ADR-009, DA09).
- El adaptador DEBE aplicar tiempo de espera corto (cada intento fallido termina en 5 s o
  menos), reintentos limitados y Circuit Breaker (Microsoft.Extensions.Http.Resilience).
- Antes de consultar APIPERU, se DEBE buscar si el paciente ya está registrado (RF03).
- Si APIPERU falla, agota su límite o el circuito está abierto, la admisión DEBE continuar
  con registro manual (RF02). El 100 % de las admisiones debe poder completarse sin APIPERU
  (EQ-03).
- Las pruebas automatizadas DEBEN usar `AdaptadorIdentidadSimulado` y nunca consumir el plan
  real de APIPERU.

**Justificación:** el servicio externo puede fallar, agotar su cuota o cambiar de proveedor
(RC13, EQ-03, EQ-09).

### VIII. Notificaciones desacopladas y sin datos clínicos

- Los módulos DEBEN publicar eventos de dominio (por ejemplo, `ResultadoLaboratorioRegistrado`)
  sin conocer a sus receptores (Observer, ADR-014). Solo el módulo Notificaciones conoce
  Web Push.
- El módulo Notificaciones decide los roles destinatarios (RF25, RF26), guarda cada aviso en
  la bandeja interna (RF27) y lo envía por Web Push con VAPID (ADR-013) en segundo plano.
- Una falla o demora del envío push NUNCA DEBE impedir ni retrasar el registro clínico que
  originó el evento (EQ-10).
- El contenido de una notificación push NO DEBE incluir datos clínicos ni personales del
  paciente: solo el tipo de evento y un enlace interno (RC19, Ley N° 29733).
- Web Push requiere HTTPS, Service Worker y permiso explícito del usuario (RC20).

**Justificación:** avisar a tiempo sin acoplar módulos ni exponer datos sensibles (DA11).

### IX. Solo datos ficticios y protección de datos personales (NO NEGOCIABLE)

- Pruebas, semillas, demostraciones, capturas y ejemplos de documentación DEBEN usar solo
  datos ficticios pero realistas (RC14). NUNCA se cargan datos reales de pacientes.
- Los DNI de prueba no deben corresponder intencionalmente a personas reales; las pruebas
  con APIPERU real se limitan a verificaciones manuales mínimas.
- Los datos de salud son sensibles (Ley N° 29733, RC15): se aplica mínimo privilegio por
  rol y no se registran datos clínicos en logs.

**Justificación:** cumplimiento normativo y repositorio público.

### X. Pruebas automatizadas antes de terminar

- Ninguna funcionalidad se da por terminada sin pruebas automatizadas que pasen y cubran:
  las reglas de dominio (incluidas RN-01 a RN-06 cuando apliquen) y los casos de uso, con
  sus caminos alternativos y de error.
- Las pruebas de dominio y de casos de uso NO DEBEN requerir PostgreSQL, Redis ni servicios
  externos: usan dobles en memoria (`RepositorioHistoriasEnMemoria`, adaptadores simulados).
- Los cambios en autorización DEBEN incluir pruebas de acceso permitido y denegado por rol.
- Los procesos Python (ETL, alerta) DEBEN tener pruebas de su lógica de transformación y
  detección.

**Justificación:** el código se genera con IA a partir de especificaciones (RC02); las
pruebas verifican que cumple las reglas clínicas.

### XI. Ubicación del código y despliegue con Docker Compose

- Todo el código fuente (backend, frontend, ETL, alerta, scripts de base de datos,
  Dockerfiles y `docker-compose.yml`) DEBE ubicarse dentro de la carpeta `tecnologia/`.
  La carpeta `docs/` contiene solo documentación.
- El sistema DEBE desplegarse con Docker Compose (ADR-010, RC09): cada servicio en su
  contenedor, con healthcheck y `restart: always`. No se usa Kubernetes.
- Todo el sistema DEBE poder levantarse localmente con un solo `docker compose up` y un
  `.env` completado a partir de `.env.example`.

**Justificación:** operación por una sola persona con bajo costo (DA04, DA06, RC05).

### XII. La documentación de docs/ es la fuente de verdad

- Las especificaciones, planes y tareas DEBEN ser coherentes con `docs/` (análisis,
  arquitectura, diseño y modelo C4).
- Si una especificación, plan o tarea contradice `docs/`, o si se detecta una contradicción
  dentro de `docs/`, se DEBE señalar explícitamente y resolver ANTES de implementar:
  actualizando `docs/` (con su commit) o corrigiendo la especificación.
- Las especificaciones DEBEN citar los identificadores que implementan (RF, RN, RC, DA,
  ADR, EQ) para mantener la trazabilidad.

**Justificación:** desarrollo guiado por especificaciones (RC02) con una sola fuente de
verdad.

## Restricciones tecnológicas y normativas

- **Backend:** C# / .NET (ASP.NET Core), EF Core + Npgsql, StackExchange.Redis,
  Microsoft.Extensions.Http.Resilience, librería Web Push para .NET.
- **Frontend:** React (SPA) con Service Worker para Web Push; instalable como PWA.
- **Datos:** PostgreSQL (`sigh_oltp`, `sigh_olap`), Redis como caché.
- **Analítica:** Python + cron (ETL y alerta epidemiológica), Metabase conectado solo a
  `sigh_olap` (ADR-011).
- **Herramientas:** solo open source o gratuitas (RC06); presupuesto personal en AWS
  (Free Tier y luego ~US$ 28/mes, RC05).
- **Normativa:** Ley N° 29733 (datos personales), Ley N° 30024 (HCE / RENHICE, sin
  integración en la prueba de concepto), diagnósticos codificados con CIE-10 (RC17).
- **Calidad (metas propuestas por validar):** 95 % de operaciones clínicas ≤ 2 s con 100
  usuarios (EQ-01); recuperación de contenedor ≤ 60 s (EQ-03); RPO ≤ 15 min y RTO ≤ 4 h
  (EQ-07); 100 % de intentos no autorizados rechazados y auditados (EQ-04).
- **Control de versiones:** Git y GitHub (RC12).

## Flujo de desarrollo y puertas de calidad

- El trabajo sigue Spec-Driven Development (RC02): especificar → aclarar → planificar →
  tareas → implementar, con los comandos de Spec Kit.
- Cada plan DEBE incluir una verificación contra esta constitución (Constitution Check) y
  justificar por escrito cualquier excepción.
- Antes de dar una funcionalidad por terminada se DEBE verificar: pruebas automatizadas en
  verde (X), ausencia de secretos en los cambios (VI), ningún acceso a tablas de otro módulo
  (II), endpoints bajo `/api/v1` con JWT y RBAC (III) y coherencia con `docs/` (XII).
- El trabajo se realiza en ramas y se integra a `main` mediante pull request.

## Gobierno

- Esta constitución prevalece sobre cualquier otra práctica, plantilla o especificación del
  proyecto. Ante conflicto con `docs/`, se aplica el principio XII.
- **Enmiendas:** se proponen por escrito indicando el principio afectado, la justificación y
  el documento de `docs/` relacionado (ADR nuevo o actualizado si cambia una decisión
  arquitectónica), y se registran con un commit propio.
- **Versionado semántico:** MAJOR al eliminar o redefinir un principio de forma incompatible;
  MINOR al agregar un principio o sección, o ampliar materialmente una guía; PATCH en
  aclaraciones y correcciones de redacción.
- **Cumplimiento:** cada plan, revisión de código y pull request DEBE verificar el
  cumplimiento de los principios. Los principios marcados como NO NEGOCIABLE no admiten
  excepciones.

**Versión**: 1.0.1 | **Ratificada**: 2026-11-26 | **Última enmienda**: 2026-11-26
