# Sistema Integral de Gestión Hospitalaria (SIGH)

## Nombre
Roger Eduardo Rodriguez Delgadillo

## Descripción
Sistema académico de gestión hospitalaria cuyo módulo núcleo son las **Historias Clínicas Electrónicas (HCE)**, alrededor del cual se articulan admisión y triaje, farmacia, laboratorio, reportes epidemiológicos y explotación analítica (OLAP / BI).

Es un proyecto individual desarrollado bajo el estándar **Spec-Driven Development (SDD)**: primero se redactan especificaciones detalladas de cada módulo y, a partir de ellas, se genera el código con apoyo de inteligencia artificial (Claude Code).

La arquitectura está diseñada para escalar a nivel empresarial, pero se despliega como prueba de concepto a escala reducida y de bajo costo, sin eliminar ningún módulo funcional ni requisito no funcional.

## Caso de estudio
Hospital regional como referencia funcional. Sistemas HCE de referencia: OpenMRS y OpenEMR.

## Curso
Arquitectura de Software

## Docente
Jaico Quispe, Lizbeth 

## Universidad
Universidad Nacional de San Cristóbal de Huamanga (UNSCH) - Escuela Profesional de Ingeniería de Sistemas

---

## Módulos funcionales
1. **Admisión y Triaje**: registro de pacientes y clasificación de urgencia.
2. **Historias Clínicas Electrónicas (HCE)**: módulo núcleo; registro, consulta y actualización del historial médico con control de acceso por rol.
3. **Farmacia y Laboratorio**: inventario de medicamentos, órdenes y resultados de exámenes vinculados a la HCE.
4. **Reportes Epidemiológicos**: reportes agregados por diagnóstico, zona y periodo.
5. **Explotación OLAP / Business Intelligence**: cubos analíticos y dashboards, separados físicamente de la base transaccional.

## Tecnologías previstas
| Capa | Tecnología |
|---|---|
| Backend | C# / .NET |
| Base de datos transaccional (OLTP) | PostgreSQL |
| Caché | Redis |
| ETL | Scripts Python programados (cron) |
| Analítica (OLAP / BI) | PostgreSQL (vistas materializadas / datamart) + Metabase o Apache Superset |
| Seguridad | JWT, TLS, AWS WAF (capa básica), Fail2ban |
| Observabilidad | Grafana / Prometheus |
| Contenedores | Docker Compose |
| Nube | AWS (EC2, EBS, S3) |

---

## Estructura del repositorio

### 01 - Análisis del sistema
- [Actores](docs/01-analisis-de-sistema/01-actores.md)
- [Historias de usuario](docs/01-analisis-de-sistema/02-historias-del-usuario.md)
- [Requisitos funcionales](docs/01-analisis-de-sistema/03-requisitos-funcionales.md)
- [Atributos de calidad](docs/01-analisis-de-sistema/04-atributos-de-calidad.md)
- [Restricciones](docs/01-analisis-de-sistema/05-restricciones.md)
- [Drivers arquitectónicos](docs/01-analisis-de-sistema/06-driver-arquitectonicos.md)
- [Decisiones arquitectónicas](docs/01-analisis-de-sistema/07-decisiones-arquitectonicas.md)
- [Escenarios de atributos de calidad](docs/01-analisis-de-sistema/08-escenarios-atributos-calidad.md)

### 02 - Arquitectura de software
- [Enfoque arquitectónico](docs/02-arquitectura-software/enfoque-arquitectonico.md)
- [Estilo arquitectónico](docs/02-arquitectura-software/estilo-arquitectonico.md)
- [Arquitectura inicial](docs/02-arquitectura-software/arquitectura-inicial.md)
- [Componentes arquitectónicos](docs/02-arquitectura-software/componentes-arquitectonicos.md)

### 03 - Diseño de software
- [Diseño interno de módulos](docs/03-diseño-de-software/diseño-interno/diseño-interno-de-modulos.md)
- [Patrones de diseño](docs/03-diseño-de-software/diseño-interno/patrones-de-diseño.md)
- [Principios de diseño](docs/03-diseño-de-software/diseño-interno/principios-de-diseño.md)

### 04 - Modelo C4
- [Nivel 1 - Diagrama de contexto](docs/04-modelo-c4/Nivel1-DiagramadeContextodelSistema.md)
- [Nivel 2 - Diagrama de contenedores](docs/04-modelo-c4/Nivel2-DiagramadeContenedores.md)
- [Nivel 3 - Diagrama de componentes](docs/04-modelo-c4/Nivel3-Diagrama-de-Componentes.md)
- [Nivel 4 - Diagrama de código](docs/04-modelo-c4/Nivel4-DiagramadeCodigo.md)

### Otras carpetas
- `img/`: diagramas exportados (PNG) y sus fuentes editables en `img/fuentes-svg/`.
- `tecnologia/`: código base del sistema (sprints posteriores).

---

## Estado del proyecto
- [x] Estructura del repositorio
- [ ] Sprint 1: análisis, arquitectura, diseño y modelo C4
- [ ] Sprints siguientes: especificaciones SDD e implementación