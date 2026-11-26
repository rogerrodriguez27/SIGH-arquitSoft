# Restricciones

Identificar las **restricciones** que condicionan las decisiones de diseño y arquitectura del sistema.

| ID | Restricción | Descripción | Origen |
|---|---|---|---|
| RC01 | **Historias Clínicas Electrónicas como núcleo** | El sistema debe incluir obligatoriamente el módulo de HCE como núcleo, alrededor del cual se integran los demás módulos. | Docente |
| RC02 | **Spec-Driven Development (SDD)** | El desarrollo debe realizarse escribiendo primero especificaciones detalladas de cada módulo y generando el código a partir de ellas con apoyo de inteligencia artificial (Claude Code). | Docente |
| RC03 | **Sistema funcional** | Se debe entregar un sistema funcional de extremo a extremo, no solo diseño o documentación, aunque sea a escala reducida. | Docente |
| RC04 | **Desarrollo individual** | El sistema lo diseña, desarrolla, despliega y documenta una sola persona, lo que limita el alcance de la infraestructura y la complejidad operativa. | Contexto |
| RC05 | **Presupuesto personal de bajo costo** | La infraestructura debe ajustarse a un presupuesto personal (AWS Free Tier durante el primer año y luego alrededor de US$ 28 mensuales), lo que descarta soluciones empresariales como Kubernetes, SSAS o WAF comercial. | Propuesta |
| RC06 | **Herramientas open source o gratuitas** | Los componentes de BI, ETL, caché y monitoreo deben ser open source o gratuitos (Metabase, scripts Python, Redis, Grafana / Prometheus, Web Push estándar). | Propuesta |
| RC07 | **Backend en C# / .NET** | La lógica del sistema debe desarrollarse en C# sobre la plataforma .NET. | Propuesta |
| RC08 | **PostgreSQL como base de datos** | La base transaccional y el datamart analítico deben implementarse sobre PostgreSQL. | Propuesta |
| RC09 | **Contenedores con Docker Compose en AWS** | El sistema debe desplegarse en contenedores Docker orquestados con Docker Compose, sobre una o dos instancias EC2 de AWS. | Propuesta |
| RC10 | **Aplicación web** | El sistema debe ser accesible mediante un navegador web. | Contexto |
| RC11 | **API REST** | La comunicación entre el frontend y el backend debe realizarse mediante una API REST definida en las especificaciones SDD. | Propuesta |
| RC12 | **Control de versiones** | La documentación y el código fuente deben gestionarse con Git y mantenerse en un repositorio de GitHub. | Docente |
| RC13 | **Validación de DNI mediante APIPERU** | La consulta directa a RENIEC requiere un convenio institucional; por indicación del docente, la validación del DNI se realiza mediante el servicio APIPERU, que actúa como intermediario. El servicio exige un token de acceso y tiene un límite de consultas según el plan contratado. | Docente / Normativa |
| RC14 | **Datos de prueba ficticios** | No se deben usar datos reales de pacientes; las pruebas y la demostración se realizan con datos ficticios pero realistas. | Normativa |
| RC15 | **Protección de datos personales** | Los datos de salud son datos sensibles según la Ley N° 29733, Ley de Protección de Datos Personales, por lo que su tratamiento exige medidas de seguridad, confidencialidad y control de acceso. | Normativa |
| RC16 | **Historia clínica electrónica** | El diseño debe respetar los principios de la Ley N° 30024, que crea el Registro Nacional de Historias Clínicas Electrónicas (RENHICE), y su reglamento: confidencialidad, integridad, disponibilidad y trazabilidad de la historia clínica. La prueba de concepto no se integra con el RENHICE. | Normativa |
| RC17 | **Codificación CIE-10** | Los diagnósticos deben registrarse con la Clasificación Internacional de Enfermedades (CIE-10), estándar usado por el MINSA para el registro y la vigilancia epidemiológica. | Normativa |
| RC18 | **Gestión de secretos fuera del repositorio** | El repositorio es público, por lo que ninguna credencial (token de APIPERU, claves VAPID, clave de firma JWT, contraseñas de base de datos) puede guardarse en el código ni en la documentación. Se configuran como variables de entorno en un archivo `.env` local, ignorado por Git, y en el servidor; el repositorio solo contiene la plantilla `.env.example`. | Contexto |
| RC19 | **Notificaciones sin datos clínicos** | Por la Ley N° 29733, las notificaciones push no deben contener datos clínicos ni personales del paciente, porque se muestran en la pantalla del dispositivo, donde pueden verlas otras personas. | Normativa |
| RC20 | **Requisitos técnicos de Web Push** | Las notificaciones push del navegador requieren HTTPS, un Service Worker en la aplicación web y el permiso explícito del usuario. En iPhone solo funcionan si la aplicación se instala como PWA (iOS 16.4 o superior). | Contexto |