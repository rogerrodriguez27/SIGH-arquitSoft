# 05 · Validación y evidencias (Sprint 2)

Dónde está cada entregable y cada actividad del Sprint 2.

## Entregables

| Entregable | Dónde |
|---|---|
| Código fuente organizado según la arquitectura | [tecnologia/](../../tecnologia/): `backend/` (monolito modular .NET 10 con Clean Architecture), `frontend/` (React) y `docker-compose.yml`. Explicación: [03-solid-y-patrones-implementados.md](03-solid-y-patrones-implementados.md) |
| Primera versión funcional | Módulo Administración: inicio de sesión, usuarios y roles, permisos, bitácora. Se levanta con `docker compose up` ([quickstart](../../specs/001-admin-auth-rbac/quickstart.md)) |
| Documentación de la API con OpenAPI / Swagger | Swagger UI en `http://localhost:8080/swagger` y documento en `/openapi/v1.json`, generados desde el código. Contrato de diseño: [api-administracion.openapi.yaml](../../specs/001-admin-auth-rbac/contracts/api-administracion.openapi.yaml). Copia generada: [resultados/openapi-v1.json](resultados/openapi-v1.json). Captura: [resultados/swagger-ui.jpg](resultados/swagger-ui.jpg) |
| Pruebas automatizadas y reporte de cobertura | [04-pruebas-y-cobertura.md](04-pruebas-y-cobertura.md): 348 pruebas de backend (95,9 % de líneas) y 27 de frontend (92,9 %) |
| Evidencias de los atributos de calidad | [01-evidencias-atributos-calidad.md](01-evidencias-atributos-calidad.md) |

## Actividades

| Actividad | Dónde |
|---|---|
| Implementar el estilo arquitectónico (monolito modular) | [03-solid-y-patrones-implementados.md §1](03-solid-y-patrones-implementados.md#1-estilo-monolito-modular-adr-001) |
| Aplicar el enfoque (Clean Architecture) | [03-solid-y-patrones-implementados.md §2](03-solid-y-patrones-implementados.md#2-enfoque-clean-architecture-adr-002) |
| Especificaciones SDD con criterios de aceptación y tareas técnicas | [specs/001-admin-auth-rbac/](../../specs/001-admin-auth-rbac/): `spec.md` (historias y criterios), `plan.md`, `research.md`, `data-model.md`, `contracts/` y `tasks.md` (122 tareas) |
| Endpoints REST funcionales y documentados | 13 operaciones bajo `/api/v1`, en Swagger UI |
| Análisis de la base de datos y estrategia de caché | [02-analisis-base-de-datos-y-cache.md](02-analisis-base-de-datos-y-cache.md) |
| SOLID y patrones de diseño | [03-solid-y-patrones-implementados.md](03-solid-y-patrones-implementados.md) |
| Pruebas automáticas de funcionalidades y reglas de negocio | [04-pruebas-y-cobertura.md](04-pruebas-y-cobertura.md) |
| CI/CD con GitHub Actions | [.github/workflows/ci.yml](../../.github/workflows/ci.yml) |
| Pruebas de los escenarios de calidad (k6, xUnit) | [tecnologia/pruebas-calidad/](../../tecnologia/pruebas-calidad/) y [01-evidencias-atributos-calidad.md](01-evidencias-atributos-calidad.md) |
