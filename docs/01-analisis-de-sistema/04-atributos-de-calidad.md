# Atributos de calidad

| ID | Atributo de calidad | Escenario de calidad | Origen |
|---|---|---|---|
| AC01 | **Rendimiento** | Las operaciones clínicas (búsqueda de pacientes, consulta y registro en la historia clínica, triaje, recetas) deben responder en menos de 2 segundos, incluso con alta concurrencia. | RNF-02 |
| AC02 | **Escalabilidad** | El sistema debe estar diseñado para soportar más de 1000 sesiones simultáneas, creciendo mediante la incorporación de nuevas instancias (escalamiento horizontal) sin cambiar su diseño lógico. | RNF-01, RNF-04 |
| AC03 | **Disponibilidad** | El sistema debe estar disponible el 99.5% del tiempo y recuperarse automáticamente ante la caída de alguno de sus servicios, ya que un hospital opera las 24 horas. | RNF-03 |
| AC04 | **Seguridad** | La información clínica y personal de los pacientes debe estar protegida frente a accesos no autorizados, garantizando su confidencialidad, su integridad (los registros firmados no se alteran) y la trazabilidad de cada acceso. | RNF-05 |
| AC05 | **Aislamiento de cargas (OLTP / OLAP)** | Las consultas analíticas, los reportes, el ETL y la alerta epidemiológica no deben degradar el rendimiento de la operación clínica diaria. | RNF-09 |
| AC06 | **Recuperabilidad** | Ante una falla grave o pérdida de datos, el sistema debe poder restaurarse perdiendo como máximo 15 minutos de información (RPO) y en un tiempo máximo de 4 horas (RTO). | RNF-11 |
| AC07 | **Observabilidad** | El estado de los servicios, los errores y los tiempos de respuesta deben poder monitorearse y rastrearse de forma centralizada. | RNF-12 |
| AC08 | **Mantenibilidad** | El sistema debe organizarse en módulos independientes, de modo que se puedan agregar, modificar o reemplazar funcionalidades (por ejemplo, el servicio de RENIEC simulado o el módulo de alertas) sin afectar a los demás. | Propuesta: SDD y modularidad |

---

## Cumplimiento a escala reducida

El diseño se orienta a las metas empresariales; la prueba de concepto demuestra los mecanismos con infraestructura de bajo costo.

| ID | Meta de diseño | Cómo se demuestra en la prueba de concepto |
|---|---|---|
| AC01 | Respuesta menor a 2 segundos | Caché con Redis para consultas frecuentes; validación con pruebas de carga ligeras. |
| AC02 | Más de 1000 sesiones simultáneas | Servicios separados en contenedores (Docker Compose) y autenticación sin estado (JWT), listos para migrar a Kubernetes; pruebas de carga con decenas o cientos de sesiones simuladas. |
| AC03 | 99.5% de disponibilidad | Healthchecks y reinicio automático de contenedores (`restart: always`); registro manual de pacientes si RENIEC no responde (RF02). Se demuestra el mecanismo, no el SLA en producción. |
| AC04 | Protección de la información clínica | TLS obligatorio, JWT, control de acceso por rol, registro de accesos (RF24), AWS WAF en capa básica y Fail2ban o Security Groups como IDS ligero. |
| AC05 | Separación física OLTP / OLAP | Base transaccional y datamart en esquemas o instancias separadas; el ETL y las alertas solo leen o escriben en el datamart. |
| AC06 | RPO ≤ 15 min y RTO ≤ 4 h | Backups automáticos en S3 (snapshots + WAL), documentados con esos objetivos como meta de diseño. |
| AC07 | Monitoreo y trazabilidad | Logs centralizados y métricas básicas con Grafana / Prometheus en contenedor. |
| AC08 | Módulos independientes | Especificaciones SDD por módulo, separación de responsabilidades e interfaces para servicios externos. |