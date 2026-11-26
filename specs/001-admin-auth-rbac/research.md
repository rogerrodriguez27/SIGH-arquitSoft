# Investigación (Fase 0): Administración — autenticación, usuarios, roles y bitácora

Cada decisión se registra con el formato **Decisión / Justificación / Alternativas consideradas**.
No quedan puntos "NEEDS CLARIFICATION" del contexto técnico.

---

## D1. Plataforma: .NET 10 LTS con controladores

- **Decisión**: el backend usa ASP.NET Core 10 con controladores (`[ApiController]`) y la solución
  `Sigh.slnx`. Las versiones se fijan de forma centralizada con `Directory.Packages.props`.
- **Justificación**:
  - .NET 10 es la versión LTS vigente, con soporte hasta noviembre de 2028.
  - Los controladores coinciden con `docs/03-diseño-de-software` (`AtencionesController`).
  - Las versiones centralizadas evitan que cada proyecto use una versión distinta de EF Core.
- **Alternativas**:
  - .NET 8 LTS: más antiguo y con soporte más corto.
  - Minimal APIs: no siguen el diseño documentado.

## D2. Hash de contraseñas: `PasswordHasher<T>` detrás de un puerto

- **Decisión**:
  - El dominio declara el puerto `IHasherContrasenas` con dos operaciones: `Calcular(texto)` y
    `Verificar(hash, texto)`.
  - La infraestructura lo implementa con `Microsoft.AspNetCore.Identity.PasswordHasher<T>`
    (PBKDF2-HMAC-SHA512, sal aleatoria e iteraciones según la versión del formato).
  - La política de contraseñas (8 caracteres o más, con letras y números, FR-005) vive en el dominio,
    en el objeto de valor `ContrasenaPropuesta`.
- **Justificación**:
  - Es un algoritmo irreversible, estándar y mantenido por Microsoft (FR-005).
  - Si el formato de hash cambia, `Verificar` informa que hay que volver a calcularlo.
  - El dominio sigue sin depender de paquetes externos (principio I).
- **Alternativas**:
  - BCrypt.Net: un paquete adicional de terceros, sin ventajas para este caso.
  - ASP.NET Core Identity completo: impone su propio modelo de usuarios y tablas, y choca con el
    dominio propio y con la matriz de permisos fija.

## D3. FR-022 entre módulos: conexión y transacción compartidas por petición

- **Decisión**:
  - `Sigh.Compartido` define:
    - `ConexionOltp`: un `NpgsqlConnection` por petición, con ámbito *scoped*.
    - `IUnidadDeTrabajo`: `EjecutarEnTransaccionAsync(Func<Task>)` abre la transacción en esa conexión.
  - Cada `DbContext` de cada módulo se configura con esa conexión y se une a la transacción activa
    (`Database.UseTransactionAsync`) antes de guardar.
  - La HCE guarda la atención en el esquema `hce` y llama a
    `IServicioBitacora.RegistrarAccesoAsync(...)`, que inserta en `administracion.bitacora_accesos_hce`.
    Las dos escrituras se confirman o se revierten juntas.
  - Los **intentos denegados** (FR-020) y los **eventos de seguridad** se registran con
    `RegistrarIntentoDenegadoAsync`, que usa su propia conexión y su propia transacción. Así quedan
    guardados aunque la operación que los causó no se confirme.
- **Justificación**:
  - Es atómico de verdad (FR-022), sin coordinador distribuido, porque todo vive en la misma base
    `sigh_oltp`.
  - Ningún módulo conoce las tablas ni el `DbContext` de otro: solo el contrato `IServicioBitacora`
    (principio II).
  - Si en el futuro un módulo se separa como servicio, solo cambia la implementación del contrato.
- **Alternativas**:
  - Patrón outbox o consistencia eventual: permitiría una atención sin auditoría durante un tiempo, y
    FR-022 lo prohíbe.
  - Un `DbContext` único para todo el sistema: rompe el aislamiento de los módulos.
  - `TransactionScope` ambiental: menos explícito, con riesgo de promoción a transacción distribuida
    y de errores silenciosos con código asíncrono.

## D4. JWT y autorización por permiso

- **Decisión**:
  - JWT HS256 firmado con `Jwt__ClaveFirma` (al menos 32 bytes; la API no arranca si falta o es más
    corta). Emisor y audiencia vienen de la configuración. Duración: 30 minutos, con `ClockSkew` de
    30 s.
  - *Claims*:
    - `sub` (id del usuario), `name` y `role` (uno por rol);
    - `perm` (uno por permiso efectivo, calculado con la matriz fija al iniciar sesión);
    - `cambio_contrasena_pendiente` cuando corresponde.
  - Autorización:
    - una política de ASP.NET Core por cada código de permiso (`[Authorize(Policy = Permisos.GestionarUsuarios)]`);
    - una política de respaldo que exige un usuario autenticado en todos los endpoints;
    - un requisito global que bloquea todo, excepto cambiar la contraseña y cerrar sesión, mientras
      `cambio_contrasena_pendiente` esté presente (FR-006).
- **Justificación**:
  - Cumple con una API sin estado (DA07) y con RBAC (ADR-007).
  - Como los permisos son fijos (aclaración Q1 = A), viajan en el token y no requieren consultar la
    base en cada petición, lo que ayuda a cumplir SC-007.
  - Un cambio de roles se refleja en la siguiente sesión, como dice la spec.
- **Alternativas**:
  - Consultar los permisos en la base en cada petición: más carga, sin beneficio porque la matriz no
    cambia en ejecución.
  - Tokens de refresco: quedan fuera de alcance; la spec prefiere sesiones cortas.

## D5. Inmutabilidad de la bitácora y de los eventos de seguridad

- **Decisión**: hay tres capas de protección.
  1. El dominio no expone métodos para modificar estas entidades.
  2. Los repositorios solo tienen `AgregarAsync`.
  3. La migración crea un *trigger* `BEFORE UPDATE OR DELETE` que lanza un error en
     `bitacora_accesos_hce` y `eventos_seguridad`, y también `REVOKE UPDATE, DELETE` para el rol de
     base de datos de la aplicación.
- **Justificación**: FR-021 y SC-003 exigen que no se pueda alterar la bitácora "por ninguna vía",
  incluido un script o un error de programación.
- **Alternativas**: confiar solo en el código. Se descarta porque no protege contra SQL manual.

## D6. Bloqueo de cuenta y mensajes genéricos

- **Decisión**:
  - El contador de intentos fallidos y la fecha de fin del bloqueo viven en `Usuario` (dominio).
  - Al quinto fallo consecutivo, la cuenta se bloquea 15 minutos. Un inicio exitoso reinicia el
    contador.
  - Para un usuario inexistente igual se calcula un hash de referencia, para que el tiempo de
    respuesta no revele si el usuario existe.
  - La API responde siempre `401` con el mismo mensaje: "Usuario o contraseña incorrectos" (FR-003).
  - El evento de seguridad interno sí distingue el motivo.
- **Justificación**: cumple FR-003 y FR-004 y evita la enumeración de usuarios.
- **Alternativas**: un bloqueo por IP con un limitador de tasa. Se puede agregar después con el
  middleware `RateLimiter`. No reemplaza el bloqueo por cuenta.

## D7. Administrador inicial desde el entorno

- **Decisión**:
  - Al arrancar, después de aplicar las migraciones, si no existe ningún usuario se crea uno con
    `AdminInicial__Usuario`, `AdminInicial__NombreCompleto` y `AdminInicial__Contrasena`, con el rol
    Administrador y el cambio de contraseña obligatorio.
  - Si alguna de esas variables falta, no se crea y se escribe una advertencia en el log, sin
    mostrar valores.
- **Justificación**: FR-016 y el principio VI: no hay credenciales en el código.
- **Alternativas**: un script SQL con un hash fijo. Se descarta porque dejaría una credencial
  conocida en el repositorio.

## D8. Consulta de la bitácora con buen rendimiento

- **Decisión**:
  - Se usa paginación por número de página (`pagina` y `tamano`, máximo 100) con orden
    `fecha_hora DESC`.
  - Índices: `(fecha_hora DESC)`, `(usuario_id, fecha_hora DESC)` y `(historia_id, fecha_hora DESC)`.
  - El rango de fechas es obligatorio y de un año como máximo (FR-024). Esto limita el recorrido.
- **Justificación**: SC-006 pide 2 s o menos con 100 000 entradas, y estos índices cubren los filtros
  de la HU16.
- **Alternativas**: paginación por cursor (*keyset*). Es más eficiente con millones de filas, pero
  innecesaria a esta escala. Puede adoptarse después sin cambiar el contrato.

## D9. Frontend

- **Decisión**:
  - React 19 + TypeScript + Vite + React Router.
  - El JWT se guarda en `sessionStorage`: se borra al cerrar la pestaña y no requiere cookies. El
    menú se construye con los *claims* `perm`.
  - La interfaz y los mensajes están en español.
  - En la imagen Docker, el frontend se sirve con nginx.
- **Justificación**:
  - ADR-003 (SPA separada).
  - La API ya usa `Authorization: Bearer` (ADR-007).
  - Ocultar opciones según los permisos mejora la experiencia, pero la seguridad real está en la API.
- **Alternativas**:
  - Cookie `HttpOnly`: más resistente a XSS, pero obliga a usar CSRF y cambia el contrato de la API.
    Se reevalúa antes de pasar a producción.

## D10. Pruebas

- **Decisión**:
  - **Dominio**: xUnit puro.
  - **Casos de uso**: dobles en memoria de repositorios, hasher y reloj (`IReloj`).
  - **API**: `WebApplicationFactory` con repositorios en memoria y una prueba de autorización por
    cada fila de la matriz (permitido y denegado), para cumplir SC-001.
  - **Integración**: Testcontainers (`postgres:17`) para migraciones, *triggers* de inmutabilidad
    (SC-003) y la transacción compartida de D3.
  - **Frontend**: Vitest + React Testing Library, para el inicio de sesión y las rutas protegidas.
- **Justificación**: principio X. Las pruebas de dominio y de casos de uso no necesitan Docker.
- **Alternativas**: SQLite en memoria para la integración. Se descarta porque no reproduce los
  *triggers* ni el comportamiento de PostgreSQL.

## D11. Variables de entorno nuevas

Se agregan a `.env.example` con valores vacíos (los valores reales solo van en `.env`):

```text
POSTGRES_USER=
POSTGRES_PASSWORD=
POSTGRES_DB=sigh_oltp
ConnectionStrings__SighOltp=
Jwt__ClaveFirma=
Jwt__Emisor=sigh-api
Jwt__Audiencia=sigh-web
Cors__OrigenPermitido=http://localhost:5173
AdminInicial__Usuario=
AdminInicial__NombreCompleto=
AdminInicial__Contrasena=
```
