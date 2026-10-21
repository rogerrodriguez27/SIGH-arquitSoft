# Restricciones

Identificar las **restricciones** que condicionan las decisiones de diseño y arquitectura del sistema.

| ID | Restricción | Descripción | Origen |
|---|---|---|---|
| RC01 | **Historias Clínicas Electrónicas como núcleo** | El sistema debe incluir obligatoriamente el módulo de HCE como núcleo, alrededor del cual se integran los demás módulos. | Docente |
| RC02 | **Spec-Driven Development (SDD)** | El desarrollo debe realizarse escribiendo primero especificaciones detalladas de cada módulo y generando el código a partir de ellas con apoyo de inteligencia artificial (Claude Code). | Docente |
| RC03 | **Sistema funcional** | Se debe entregar un sistema funcional de extremo a extremo, no solo diseño o documentación, aunque sea a escala reducida. | Docente |
| RC04 | **Desarrollo individual** | El sistema lo diseña, desarrolla, despliega y documenta una sola persona, lo que limita el alcance de la infraestructura y la complejidad operativa. | Contexto |
| RC05 | **Presupuesto personal de bajo costo** | La infraestructura debe ajustarse a un presupuesto personal (AWS Free Tier durante el primer año y luego alrededor de US$ 28 mensuales), lo que descarta soluciones empresariales como Kubernetes, SSAS o WAF comercial. | Propuesta |
| RC06 | **Herramientas open source o gratuitas** | Los componentes de BI, ETL, caché y monitoreo deben ser open source o gratuitos (Metabase o Superset, scripts Python, Redis, Grafana / Prometheus). | Propuesta |
| RC07 | **Backend en C# / .NET** | La lógica del sistema debe desarrollarse en C# sobre la plataforma .NET. | Propuesta |
| RC08 | **PostgreSQL como base de datos** | La base transaccional y el datamart analítico deben implementarse sobre PostgreSQL. | Propuesta |
| RC09 | **Contenedores con Docker Compose en AWS** | El sistema debe desplegarse en contenedores Docker orquestados con Docker Compose, sobre una o dos instancias EC2 de AWS. | Propuesta |
| RC10 | **Aplicación web** | El sistema debe ser accesible mediante un navegador web. | Contexto |
| RC11 | **API REST** | La comunicación entre el frontend y el backend debe realizarse mediante una API REST definida en las especificaciones SDD. | Propuesta |
| RC12 | **Control de versiones** | La documentación y el código fuente deben gestionarse con Git y mantenerse en un repositorio de GitHub. | Docente |
| RC13 | **Integración con RENIEC sin convenio** | La consulta real al servicio de RENIEC requiere un convenio institucional; por ello, en la prueba de concepto se usa un servicio simulado con la misma interfaz. | Normativa |
| RC14 | **Datos de prueba ficticios** | No se deben usar datos reales de pacientes; las pruebas y la demostración se realizan con datos ficticios pero realistas. | Normativa |
| RC15 | **Protección de datos personales** | Los datos de salud son datos sensibles según la Ley N° 29733, Ley de Protección de Datos Personales, por lo que su tratamiento exige medidas de seguridad, confidencialidad y control de acceso. | Normativa |
| RC16 | **Historia clínica electrónica** | El diseño debe respetar los principios de la Ley N° 30024, que crea el Registro Nacional de Historias Clínicas Electrónicas (RENHICE), y su reglamento: confidencialidad, integridad, disponibilidad y trazabilidad de la historia clínica. La prueba de concepto no se integra con el RENHICE. | Normativa |
| RC17 | **Codificación CIE-10** | Los diagnósticos deben registrarse con la Clasificación Internacional de Enfermedades (CIE-10), estándar usado por el MINSA para el registro y la vigilancia epidemiológica. | Normativa |