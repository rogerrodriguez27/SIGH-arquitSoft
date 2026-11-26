# Evidencias de los atributos de calidad (Sprint 2)

## Objetivo

Registrar los resultados **medidos** de los escenarios de calidad definidos en el Sprint 1
([08-escenarios-atributos-calidad.md](../01-analisis-de-sistema/08-escenarios-atributos-calidad.md))
que aplican a la funcionalidad implementada en este sprint: el módulo **Administración**
(`specs/001-admin-auth-rbac`).

## Entorno de la prueba

| Elemento | Valor |
|---|---|
| Sistema | `docker compose up` de `tecnologia/docker-compose.yml`: `postgres` (PostgreSQL 17.12), `api` (ASP.NET Core 10, Alpine) y `web` (nginx) |
| Equipo | Windows 11 con Docker Desktop 29.5.3 (16 CPU y 7,4 GiB asignados a Docker) |
| Herramientas | k6 v2.4.0 (imagen `grafana/k6`), xUnit + Testcontainers (PostgreSQL 17), PowerShell, gitleaks |
| Datos | Solo datos ficticios (principio IX): usuarios `k6…` creados por cada prueba y 100 000 entradas generadas en la bitácora |
| Fecha | 26/11/2026 |

Cómo repetir las pruebas: [tecnologia/pruebas-calidad/README.md](../../tecnologia/pruebas-calidad/README.md).

## Resumen

| Escenario | Atributo | ¿Aplica en este sprint? | Herramienta | Medida esperada | Resultado medido | Estado |
|---|---|---|---|---|---|---|
| EQ-01 | Rendimiento | Parcial: aún no hay operaciones clínicas; se miden el inicio de sesión y las operaciones protegidas del módulo | k6 + xUnit | p95 ≤ 2 s y < 1 % de errores con 100 usuarios simultáneos | Inicio de sesión: **p95 = 572 ms**. Operaciones: **p95 = 20 ms**. **0 %** de errores | ✅ Cumple |
| EQ-02 | Escalabilidad | Parcial: con una instancia | k6 | Hasta 200 usuarios, p95 < 3 s y < 1 % de errores | **p95 = 17 ms**, **0 %** de errores, 332 peticiones/s con 200 usuarios | ✅ Cumple (1 instancia). La comparación con 2 instancias está pendiente |
| EQ-03 | Disponibilidad | Parcial: reinicio de la API (APIPERU aún no existe) | PowerShell + Docker | Recuperación automática en 60 s o menos | **2,2 s** en los 3 intentos | ✅ Cumple |
| EQ-04 | Seguridad (confidencialidad) | Sí | k6 + xUnit | 100 % de los intentos no autorizados rechazados y registrados | **54/54** rechazados en el sistema desplegado; **104/104** combinaciones rol × permiso en xUnit; cada 403 queda como `ACCESO_DENEGADO` | ✅ Cumple |
| EQ-05 | Seguridad (integridad) | No: los registros clínicos firmados llegan con la HCE | — | — | La misma técnica ya protege la bitácora (ver la nota) | ⏳ Pendiente |
| EQ-06 | Aislamiento de cargas | No: aún no hay datamart ni ETL | — | — | — | ⏳ Pendiente |
| EQ-07 | Recuperabilidad | No: backups en S3 en el despliegue en AWS | — | — | — | ⏳ Pendiente |
| EQ-08 | Observabilidad | No: Prometheus y Grafana aún no están desplegados | — | — | — | ⏳ Pendiente |
| EQ-09 | Mantenibilidad | Parcial: reglas de dependencia | xUnit | Cambios concentrados en su capa o módulo | La prueba de arquitectura verifica la regla de dependencia; los otros módulos solo ven `IServicioBitacora` | ✅ Cumple lo aplicable |
| EQ-10 | Disponibilidad (notificaciones) | No: módulo de Notificaciones pendiente | — | — | — | ⏳ Pendiente |
| EQ-11 | Seguridad (credenciales) | Sí | gitleaks + Git + CI | 0 credenciales en el repositorio y su historial | **0** hallazgos en 53 commits; `.env` ignorado; búsqueda automática en cada PR | ✅ Cumple |

## EQ-01. Rendimiento

**Prueba:** [eq01-rendimiento.js](../../tecnologia/pruebas-calidad/k6/eq01-rendimiento.js). Tiene dos escenarios:

1. **100 inicios de sesión simultáneos** (SC-007): los 100 usuarios virtuales inician sesión a la vez, cada uno con un usuario distinto.
2. **100 usuarios con sesión durante 2 minutos**: cada uno lista usuarios, consulta la matriz de permisos y consulta la bitácora, con 1 s de pausa entre ciclos.

| Métrica | Inicio de sesión | Operaciones |
|---|---|---|
| Promedio | 427 ms | 11,5 ms |
| Mediana | 466 ms | 9 ms |
| p95 | **572 ms** | **20 ms** |
| Máximo | 588 ms | 220 ms |
| Errores | 0 % | 0 % (0 de 35 662 peticiones) |

- El inicio de sesión tarda más a propósito: la contraseña se verifica con PBKDF2 (FR-005, research D2).
- Las operaciones son rápidas porque los permisos viajan en el JWT (research D4), así que autorizar una petición no requiere ir a la base de datos.
- **Confirmación con xUnit:** `InicioSesionCargaTests` (100 inicios simultáneos con PostgreSQL real) mide **p95 = 654 ms**.

Salida completa: [resultados/eq01-rendimiento.txt](resultados/eq01-rendimiento.txt) y [.json](resultados/eq01-rendimiento.json).

### Hallazgo corregido gracias a esta prueba

En la primera ejecución, 2 de los 100 inicios de sesión fallaron con **409**. La investigación mostró
un defecto real: si el **mismo usuario** inicia sesión dos veces al mismo tiempo (doble clic, dos
pestañas), ambas peticiones actualizan su fila y la concurrencia optimista rechaza la segunda. Con 6
peticiones simultáneas, 4 recibían 409.

**Corrección:** ante el conflicto, `IniciarSesionCasoUso` vuelve a leer al usuario y repite el
intento sobre el estado actual. Mientras tanto reutiliza la verificación del hash (Decorator
`HasherConMemoria`), así que cada reintento cuesta solo una lectura y una escritura. Ningún intento
fallido se pierde, lo que importa para el bloqueo de FR-004.

**Pruebas que lo cubren:** `Varios_inicios_de_sesion_simultaneos_del_mismo_usuario_funcionan_todos`
(integración) y tres pruebas del caso de uso. **Resultado tras la corrección:** 18 de 18 inicios
simultáneos del mismo usuario responden 200.

## EQ-02. Escalabilidad

**Prueba:** [eq02-escalabilidad.js](../../tecnologia/pruebas-calidad/k6/eq02-escalabilidad.js).
La carga sube de 0 a 50, 100, 150 y 200 usuarios simultáneos (30 s por escalón), se mantiene 1 minuto
con 200 usuarios y después baja.

| Métrica | Valor |
|---|---|
| Peticiones medidas | 77 232 (332 por segundo) |
| p95 | **17 ms** (límite: 3000 ms) |
| Máximo | 61,6 ms |
| Errores | **0 %** |
| CPU y memoria con 200 usuarios | API: 77 % de CPU y 143 MiB · PostgreSQL: 46 % de CPU y 235 MiB |

**Pendiente:** la comparación con **dos instancias** de la API. La API ya no guarda estado (JWT,
DA07), así que puede duplicarse, pero hace falta un balanceador de carga que reparta las peticiones,
y `docs/` todavía no lo define. Según el principio XII, primero debe documentarse en la arquitectura.
Antes de duplicar la API también hay que ajustar las conexiones (ver
[02-analisis-base-de-datos-y-cache.md](02-analisis-base-de-datos-y-cache.md#conexiones)).

## EQ-03. Disponibilidad: caída de la API

**Prueba:** [eq03-reinicio-api.ps1](../../tecnologia/pruebas-calidad/eq03-reinicio-api.ps1).
Termina el proceso principal de la API dentro de su contenedor y mide cuánto tarda `/salud` en volver
a responder 200, gracias a `restart: always` y al healthcheck.

| Intento | Dejó de responder | Docker lo reinició | Segundos hasta responder | Cumple (≤ 60 s) |
|---|---|---|---|---|
| 1 | Sí | Sí | 2,2 | Sí |
| 2 | Sí | Sí | 2,2 | Sí |
| 3 | Sí | Sí | 2,2 | Sí |

La parte de APIPERU (Circuit Breaker y registro manual) se valida cuando exista el módulo de Admisión.

## EQ-04. Seguridad: acceso no autorizado

**Prueba contra el sistema desplegado:** [eq04-seguridad.js](../../tecnologia/pruebas-calidad/k6/eq04-seguridad.js).
Para 6 endpoints protegidos se intentó:

- acceder sin sesión → se espera 401;
- acceder con un token manipulado → se espera 401;
- acceder con cada uno de los 7 roles sin permiso → se espera 403.

| Medida | Resultado |
|---|---|
| Intentos no autorizados | **54 de 54 rechazados (100 %)** |
| Respuestas de rechazo | Solo `application/problem+json` con título y estado, sin datos ni tokens |
| Controles positivos (Administrador) | 3 de 3 responden 200 |
| Rechazos registrados como `ACCESO_DENEGADO` | **42 de 42** (los 401 no tienen usuario identificado, por lo que no se pueden atribuir a nadie) |

**Pruebas automáticas que lo complementan:**

- `AutorizacionPorPermisoTests`: los 8 roles × 13 permisos (**104 combinaciones**) responden 200 o 403 exactamente según la matriz (SC-001).
- `AccesoDenegadoTests`: un farmacéutico que intenta consultar la HCE recibe 403 y queda registrado.
- `TransaccionCompartidaTests`: los accesos a historias, permitidos y denegados, quedan en la bitácora (SC-002).

## EQ-05. Integridad (nota)

Los registros clínicos firmados llegan con la HCE. La misma técnica que pide EQ-05 ya está aplicada y
probada en la bitácora y en los eventos de seguridad:

- el dominio no tiene métodos de modificación;
- los repositorios solo insertan;
- un trigger por sentencia rechaza `UPDATE`, `DELETE` y `TRUNCATE`, incluso desde SQL manual.

Pruebas: `BitacoraInmutableTests` y `EventosSeguridadInmutablesTests`, más el paso 10 del quickstart.

## EQ-09. Mantenibilidad

- `ReglaDependenciaTests` falla si el dominio llega a referenciar algo distinto de la biblioteca base y `Sigh.Compartido`.
- Los demás módulos solo pueden usar el contrato público `IServicioBitacora` (Facade).
- Las pruebas de dominio y de casos de uso usan dobles en memoria y no necesitan base de datos (principio X).

## EQ-11. Secretos fuera del repositorio

| Verificación | Resultado |
|---|---|
| `gitleaks git` sobre todo el historial | 53 commits revisados: **no leaks found** |
| Búsqueda automática en cada push y PR | Trabajo `secretos` de [.github/workflows/ci.yml](../../.github/workflows/ci.yml) |
| `.env` ignorado por Git | Sí (`.gitignore`); solo se versiona `.env.example` con valores vacíos |
| Secretos en los logs de la API | `SinSecretosEnLogsTests`: 0 contraseñas, tokens o claves, incluso en nivel Trace (SC-008) |
| Evidencias de este documento | El resumen JSON de k6 se guarda sin `setup_data` (que contiene los tokens de la prueba) |
