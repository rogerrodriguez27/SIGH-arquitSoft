# Análisis del comportamiento de la base de datos y estrategia de caché (Sprint 2)

## Objetivo

Analizar con mediciones reales cómo se comporta la base transaccional `sigh_oltp` con el módulo
Administración, y definir la estrategia de caché de **ADR-006** para los módulos siguientes.

Todas las cifras salen del sistema levantado con Docker Compose y de 100 000 entradas ficticias en la
bitácora. Salida completa: [resultados/bd-explain-analyze.txt](resultados/bd-explain-analyze.txt).

## 1. Modelo y aislamiento

- El módulo usa solo el esquema `administracion` (principio II).
- Tablas: `usuarios`, `usuario_roles`, `roles`, `permisos`, `rol_permisos`, `bitacora_accesos_hce`, `eventos_seguridad` y `historial_migraciones`.
- Ninguna tabla tiene llaves foráneas hacia otros esquemas. Las referencias entre módulos, como `historia_id`, son solo identificadores.
- Las tablas `roles`, `permisos` y `rol_permisos` son una copia de la matriz fija, para informes SQL. La aplicación no las lee para autorizar: decide con `MatrizPermisos` en memoria.

## 2. Índices y planes de ejecución medidos

| # | Consulta (como la genera EF Core) | Índice usado | Plan | Tiempo |
|---|---|---|---|---|
| Q1 | Inicio de sesión: usuario por nombre, con sus roles | `ux_usuarios_nombre_usuario` + `PK_usuario_roles` | Index Scan + Index Only Scan | **0,09 ms** |
| Q2 | Bitácora: un usuario en 30 días, página 1 | `ix_bitacora_accesos_hce_usuario_fecha_hora` | Index Scan | **0,17 ms** |
| Q2b | Total de Q2 (para la paginación) | El mismo | Bitmap Index Scan | **0,14 ms** |
| Q3 | Bitácora: una historia en todo el año | `ix_bitacora_accesos_hce_historia_fecha_hora` | Bitmap Index Scan | **0,29 ms** |
| Q4 | Bitácora: todo el año, página 1 | `ix_bitacora_accesos_hce_fecha_hora` | Index Scan | **0,21 ms** |
| Q4b | Total de todo el año (peor caso) | Ninguno | **Seq Scan** de 100 000 filas | **22 ms** |
| Q5 | Denegados de 7 días con una acción | `ix_bitacora_accesos_hce_fecha_hora` + filtro | Bitmap Index Scan | **1,4 ms** |
| Q6 | Inserción de un acceso (cada acceso a la HCE) | — | Insert | **0,12 ms** |

**Conclusiones:**

1. Los tres índices de research D8 cubren los filtros de la HU16. Con la prueba de carga de EQ-02
   (77 232 peticiones), el índice por fecha se usó 98 998 veces y el índice único por nombre de
   usuario, 26 375 veces (`pg_stat_user_indexes`).
2. El único recorrido completo es el **total de un rango amplio sin filtros** (Q4b). Su costo crece
   en proporción al volumen: 22 ms con 100 000 filas, unos 220 ms con un millón. Está dentro de la
   meta (SC-006: p95 ≤ 2 s). Si la bitácora pasa de unos pocos millones de filas, hay dos opciones
   ya previstas en research D8: paginación por cursor (*keyset*) o un total estimado.
3. **Tamaño:** 100 000 entradas ocupan 31 MB, de los cuales 17 MB son índices (unos 310 bytes por
   fila). Un millón de accesos ocuparía unos 310 MB.

## 3. Transacciones

| Escritura | Transacción | Por qué |
|---|---|---|
| Acceso permitido a la HCE (`RegistrarAccesoAsync`) | **La misma** que la operación clínica, mediante `IUnidadDeTrabajo` y una conexión compartida por petición (research D3) | FR-022: no puede existir una atención sin su registro. Probado con un módulo simulado: si una escritura falla, se revierten las dos |
| Intento denegado (`RegistrarIntentoDenegadoAsync`) | **Propia** | FR-020: debe quedar aunque la operación se revierta |
| Eventos de seguridad | **Propia** | Deben quedar aunque la operación que los causó falle |

## 4. Concurrencia

- La tabla `usuarios` tiene la columna `version`, que funciona como *token* de concurrencia optimista: dos actualizaciones simultáneas del mismo usuario no se pisan.
- **Hallazgo de la prueba de carga (corregido):** el inicio de sesión también actualiza la fila (contador de intentos y último acceso). Varios inicios simultáneos del mismo usuario chocaban y respondían 409.
- **Corrección:** ahora el caso de uso vuelve a leer al usuario y reintenta, hasta 10 veces. Ver [01-evidencias-atributos-calidad.md](01-evidencias-atributos-calidad.md#hallazgo-corregido-gracias-a-esta-prueba).
- La bitácora es de solo inserción, así que no tiene conflictos de concurrencia: 100 registros simultáneos quedan todos guardados (`RegistrosConcurrentesTests`).

## 5. Inmutabilidad

`bitacora_accesos_hce` y `eventos_seguridad` tienen un trigger `BEFORE UPDATE OR DELETE OR TRUNCATE
… FOR EACH STATEMENT`, además de `REVOKE UPDATE, DELETE, TRUNCATE`.

El trigger es **por sentencia** porque la prueba en Docker mostró dos cosas:

- un trigger por fila no rechaza un `DELETE` que no afecta filas;
- el `REVOKE` no limita a un superusuario, que es como se conecta la aplicación en Docker.

Con el trigger por sentencia, el rechazo es total (FR-021, SC-003).

## 6. Conexiones

| Parámetro | Valor |
|---|---|
| `max_connections` de PostgreSQL | 100 (valor por defecto) |
| Pool de Npgsql por instancia de la API | Hasta 100 conexiones (valor por defecto) |
| Conexiones abiertas en reposo | 2 |
| Conexiones por petición | 1, compartida por todos los `DbContext` de la petición (research D3), más 1 breve para cada evento de seguridad |

**Riesgo detectado para EQ-02 con dos instancias:** dos instancias podrían abrir hasta 200
conexiones, más de las 100 que acepta PostgreSQL. Antes de duplicar la API hay que hacer una de dos
cosas:

- limitar el pool de cada instancia, por ejemplo con `Maximum Pool Size=40` en `ConnectionStrings__SighOltp`;
- o subir `max_connections`.

Lo que se elija se documentará con el balanceador de carga.

## 7. Crecimiento de `eventos_seguridad`

Cada consulta a la bitácora (FR-025) y cada inicio de sesión (FR-026) escriben un evento. Durante las
pruebas de carga se generaron **49 502** eventos `BITACORA_CONSULTADA`. Es lo que pide la spec, pero
significa que la tabla crece al ritmo del uso.

La spec indica que no se purga durante la prueba de concepto. Para producción convendrá definir una
política de retención o de archivo, por ejemplo particionar por mes. Es una decisión para `docs/`.

## 8. Estrategia de caché

### 8.1 Lo que ya funciona como caché en este módulo

| Técnica | Dónde | Qué evita |
|---|---|---|
| Permisos dentro del JWT | `EmisorTokensJwt`, políticas de autorización | Consultar roles y permisos en la base en cada petición. Por eso las operaciones tienen p95 de 20 ms |
| Matriz fija en memoria | `MatrizPermisos` (estática) | Leer `rol_permisos`: la matriz solo cambia con una nueva versión |
| Pool de conexiones | `NpgsqlDataSource` | Abrir una conexión TCP por petición |
| Verificación de contraseña reutilizada en los reintentos | `HasherConMemoria` (Decorator) | Repetir PBKDF2 cuando hay un conflicto de concurrencia |

**Redis no se usa todavía, y es correcto:** los datos de este módulo son de seguridad o de auditoría,
y deben leerse siempre desde la fuente.

- Si un usuario se desactiva, ese cambio no puede quedar oculto detrás de una caché.
- La bitácora debe ser exacta.

Además, las mediciones ya cumplen AC01 sin caché (p95 de 20 ms).

### 8.2 Estrategia para los módulos siguientes (ADR-006)

Según ADR-006 y el patrón Decorator de
[patrones-de-diseño.md](../03-diseño-de-software/diseño-interno/patrones-de-diseño.md), Redis se usará
para los **catálogos** (CIE-10, medicamentos, exámenes) cuando llegue esa funcionalidad (HU15, RF23).

| Aspecto | Decisión propuesta |
|---|---|
| Patrón | *Cache-aside* con Decorator: `CatalogoCie10EnCache` envuelve a `CatalogoCie10EfCore`, ambos cumplen `ICatalogoCie10`, y los casos de uso no cambian (OCP, DIP) |
| Qué se guarda | Solo datos de catálogo, que son públicos, sin datos de pacientes y de cambio poco frecuente |
| Qué **no** se guarda | Historias clínicas, recetas, resultados, bitácora, usuarios y permisos (privacidad, Ley N.° 29733, y exactitud de la auditoría) |
| Claves | Con espacio de nombres y versión, por ejemplo `sigh:cie10:v1:{codigo}` |
| Vigencia (TTL) | Larga, por ejemplo 24 horas, porque el catálogo cambia poco |
| Invalidación | Cuando el administrador modifica un catálogo, se borra la clave afectada o se sube la versión del espacio de nombres |
| Si Redis falla | Se consulta PostgreSQL directamente: la caché acelera pero nunca es obligatoria (AC03). El fallo se registra en los logs |
| Cómo se validará | Tasa de aciertos de la caché y comparación del p95 de las operaciones clínicas con y sin Redis (EQ-01), con k6 |
