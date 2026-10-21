# Drivers arquitectónicos

Los **drivers arquitectónicos** identificados para el sistema son los siguientes:

| ID | Driver arquitectónico | Origen | ¿Por qué influye en la arquitectura? | Prioridad |
|---|---|---|---|---|
| DA01 | El sistema debe tener las Historias Clínicas Electrónicas como módulo núcleo, del cual dependen triaje, farmacia, laboratorio y reportes. | RC01 – HCE como núcleo | Define el modelo de datos central (paciente, historia clínica, atención) y la dirección de las dependencias entre módulos. | Alta |
| DA02 | El sistema debe proteger la confidencialidad, integridad y trazabilidad de la información clínica de los pacientes. | AC04 – Seguridad, RC15, RC16, RF09, RF24 | Influye en la autenticación (JWT), la autorización por rol, el cifrado en tránsito (TLS), el registro de auditoría y la forma de guardar registros clínicos sin permitir su modificación. | Alta |
| DA03 | Las cargas analíticas (reportes, dashboards, ETL y alertas) no deben afectar la operación clínica diaria. | AC05 – Aislamiento OLTP / OLAP | Obliga a separar la base transaccional del datamart analítico y a comunicarlos mediante un proceso ETL. | Alta |
| DA04 | El sistema debe construirse y operarse por una sola persona con un presupuesto personal bajo y herramientas gratuitas. | RC04, RC05, RC06, RC09 | Limita la cantidad de servicios a desplegar y operar, favorece una arquitectura simple de desplegar en contenedores y descarta infraestructura empresarial costosa. | Alta |
| DA05 | Las operaciones clínicas deben responder en menos de 2 segundos con alta concurrencia. | AC01 – Rendimiento | Influye en el uso de caché (Redis), en el diseño de consultas e índices y en la separación de lecturas y escrituras. | Media |
| DA06 | El sistema debe estar disponible permanentemente y poder recuperarse ante fallas o pérdida de datos. | AC03 – Disponibilidad, AC06 – Recuperabilidad | Influye en los healthchecks, el reinicio automático de servicios, la estrategia de backups (snapshots + WAL) y los mecanismos de contingencia ante servicios externos. | Media |
| DA07 | El sistema debe poder crecer agregando instancias, sin cambiar su diseño lógico. | AC02 – Escalabilidad | Obliga a que los servicios no guarden estado de sesión (JWT, caché externa) y a empaquetarlos en contenedores portables a Kubernetes. | Media |
| DA08 | Cada módulo debe poder especificarse, generarse y modificarse de forma independiente. | AC08 – Mantenibilidad, RC02 – SDD | Influye en la separación de responsabilidades por módulo, en los contratos entre ellos y en que el módulo de alertas funcione de forma independiente. | Alta |
| DA09 | La integración con RENIEC debe poder simularse ahora y reemplazarse por el servicio real en el futuro. | RC13 – RENIEC sin convenio, RF01, RF02 | Obliga a aislar los servicios externos detrás de una interfaz, para cambiar la implementación sin afectar la lógica del sistema. | Media |
| DA10 | El sistema debe ser una aplicación web que se comunique con el backend mediante una API REST. | RC10 – Aplicación web, RC11 – API REST | Limita las alternativas de comunicación entre frontend y backend y define los contratos de API de las especificaciones SDD. | Media |

---

## Influencia de los drivers en la arquitectura

| Aspecto de la arquitectura | Drivers que lo condicionan |
|---|---|
| Organización en módulos y sus dependencias | DA01, DA04, DA08 |
| Almacenamiento de datos (OLTP, OLAP, caché) | DA01, DA03, DA05 |
| Seguridad y auditoría | DA02 |
| Despliegue e infraestructura | DA04, DA06, DA07 |
| Integración con servicios externos | DA09, DA10 |