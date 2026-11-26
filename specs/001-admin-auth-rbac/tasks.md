---

description: "Lista de tareas de la funcionalidad 001-admin-auth-rbac (módulo Administración)"
---

# Tareas: Administración — autenticación, usuarios, roles y bitácora de accesos

**Entrada**: documentos de diseño en `specs/001-admin-auth-rbac/`

**Prerrequisitos**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Pruebas**: se incluyen. Las exige la constitución (principio X), research D10 y quickstart.md. Dentro
de cada historia, las pruebas se escriben primero y deben fallar antes de implementar.

**Organización**: las tareas se agrupan por historia de usuario para que cada una se implemente y se
pruebe de forma independiente.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: se puede ejecutar en paralelo (archivos distintos, sin dependencias pendientes).
- **[Historia]**: historia de usuario a la que pertenece (US1 a US5).
- Cada descripción indica la ruta exacta del archivo.

## Convenciones de rutas

Todo el código vive en `tecnologia/` (principio XI). Abreviaturas usadas en las descripciones:

| Abreviatura | Ruta real |
|---|---|
| `back/` | `tecnologia/backend/` |
| `Api/` | `tecnologia/backend/src/Sigh.Api/` |
| `Comp/` | `tecnologia/backend/src/Compartido/Sigh.Compartido/` |
| `Dom/` | `tecnologia/backend/src/Modulos/Administracion/Sigh.Administracion.Dominio/` |
| `Apl/` | `tecnologia/backend/src/Modulos/Administracion/Sigh.Administracion.Aplicacion/` |
| `Inf/` | `tecnologia/backend/src/Modulos/Administracion/Sigh.Administracion.Infraestructura/` |
| `AdmApi/` | `tecnologia/backend/src/Modulos/Administracion/Sigh.Administracion.Api/` |
| `TDom/` | `tecnologia/backend/tests/Sigh.Administracion.Dominio.Tests/` |
| `TApl/` | `tecnologia/backend/tests/Sigh.Administracion.Aplicacion.Tests/` |
| `TApi/` | `tecnologia/backend/tests/Sigh.Api.Tests/` |
| `TInt/` | `tecnologia/backend/tests/Sigh.Administracion.Integracion.Tests/` |
| `front/` | `tecnologia/frontend/` |

Las pruebas de `TInt/` llevan `[Trait("Category", "Integracion")]` para poder filtrarlas
(quickstart.md, pasos 1 y 2). Todos los usuarios y datos de prueba son ficticios (principio IX).

---

## Fase 1: Configuración inicial (infraestructura compartida)

**Propósito**: crear la solución, los proyectos y el frontend vacíos, según la estructura de plan.md.

- [X] T001 Crear la solución `back/Sigh.slnx` con los proyectos de plan.md (`Api/`, `Comp/`, `Dom/`, `Apl/`, `Inf/`, `AdmApi/` y los cuatro proyectos de `back/tests/`) y sus referencias según la regla de dependencia: `Dom` → solo `Comp` (como en docs §6.1); `Apl` → `Dom`, `Comp`; `Inf` → `Apl`, `Dom`, `Comp`; `AdmApi` → `Apl`; `Sigh.Api` → todos. Cada prueba referencia solo la capa que prueba (más `Inf` para los dobles en memoria)
- [X] T002 [P] Crear `back/Directory.Build.props` con `net10.0`, `LangVersion` 14, `Nullable` y `ImplicitUsings` activos y `TreatWarningsAsErrors=true` (research D1)
- [X] T003 [P] Crear `back/Directory.Packages.props` con versiones centralizadas de: Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Design, Npgsql.EntityFrameworkCore.PostgreSQL, Npgsql, Microsoft.Extensions.Identity.Core, xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing y Testcontainers.PostgreSql (research D1, D2, D10)
- [X] T004 [P] Crear `Api/appsettings.json` solo con valores no secretos: `Jwt:Emisor`, `Jwt:Audiencia`, `Jwt:DuracionMinutos=30` y `Logging`. Sin cadena de conexión, sin clave de firma (principio VI)
- [X] T005 [P] Crear el proyecto frontend en `front/` con Vite + React 19 + TypeScript + React Router + Vitest + React Testing Library: `front/package.json`, `front/vite.config.ts` (puerto 5173, entorno `jsdom` para Vitest), `front/tsconfig.json`, `front/index.html`, `front/src/main.tsx` (research D9)

---

## Fase 2: Base (prerrequisitos que bloquean todas las historias)

**Propósito**: piezas compartidas del host, de `Sigh.Compartido`, de la matriz fija y de los eventos de
seguridad, que usan las cinco historias.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta terminar esta fase.

### Sigh.Compartido

- [X] T006 [P] Crear los errores de negocio `ReglaNegocioError`, `RecursoNoEncontradoError` y `ConflictoError` en `Comp/Errores/ReglaNegocioError.cs`, `Comp/Errores/RecursoNoEncontradoError.cs` y `Comp/Errores/ConflictoError.cs` (docs §6.2 y §6.4; `ConflictoError` cubre el `409` del contrato)
- [X] T007 [P] Crear el puerto `IReloj` y su implementación `RelojSistema` (UTC) en `Comp/Tiempo/IReloj.cs` y `Comp/Tiempo/RelojSistema.cs`
- [X] T008 [P] Crear el tipo genérico `Pagina<T>` (`Elementos`, `NumeroPagina`, `Tamano`, `Total`) en `Comp/Paginacion/Pagina.cs`
- [X] T009 Crear la conexión y la unidad de trabajo compartidas (research D3): `Comp/Persistencia/ConexionOltp.cs` (un `NpgsqlConnection` por petición, *scoped*, desde `ConnectionStrings__SighOltp`), `Comp/Persistencia/IUnidadDeTrabajo.cs` (`EjecutarEnTransaccionAsync(Func<Task>)`, `TransaccionActiva`) y `Comp/Persistencia/UnidadDeTrabajoNpgsql.cs`. En `Sigh.Compartido.csproj`, el paquete Npgsql va con `PrivateAssets="all"` para que no llegue de forma transitiva a `Dom` (principio I)
- [X] T010 [P] Crear el atributo marcador `PermitirConCambioContrasenaPendienteAttribute` (sin dependencias de ASP.NET Core) en `Comp/Autorizacion/PermitirConCambioContrasenaPendienteAttribute.cs` (research D4)

### Dominio: roles, permisos y matriz fija

- [X] T011 [P] Crear la enumeración fija `CodigoRol` según [data-model.md](./data-model.md) `CodigoRol` en `Dom/Roles/CodigoRol.cs` (FR-011)
- [X] T012 [P] Crear las constantes `CodigoPermiso` según [data-model.md](./data-model.md) `CodigoPermiso` (códigos y descripciones) en `Dom/Permisos/CodigoPermiso.cs`
- [X] T013 Crear `MatrizPermisos` como valor estático, traducción literal de la tabla "Matriz inicial de permisos" de spec.md, con `PermisosDe(roles)` (unión) y `Filas()`, según [data-model.md](./data-model.md) `MatrizPermisos`, en `Dom/Permisos/MatrizPermisos.cs` (FR-012, FR-013)

### Dominio y persistencia: eventos de seguridad

- [X] T014 [P] Crear la entidad inmutable `EventoSeguridad` y la enumeración `TipoEventoSeguridad`, sin métodos de modificación, según [data-model.md](./data-model.md) `EventoSeguridad.Tipo`, `EventoSeguridad.Detalle`, `EventoSeguridad.ActorId`, `EventoSeguridad.ObjetivoId` y `EventoSeguridad.FechaHora`, en `Dom/Seguridad/EventoSeguridad.cs` y `Dom/Seguridad/TipoEventoSeguridad.cs` (FR-026)
- [X] T015 [P] Crear el puerto de solo inserción `IRepositorioEventosSeguridad` (`AgregarAsync`) en `Dom/Contratos/IRepositorioEventosSeguridad.cs` (research D5)
- [X] T016 Crear `AdministracionDbContext` (esquema `administracion`, nombres `snake_case`, usa `ConexionOltp` y se une a la transacción activa con `Database.UseTransactionAsync`, research D3) y el mapeo `EventoSeguridadConfiguracion` según [data-model.md](./data-model.md) tabla `eventos_seguridad`, en `Inf/Persistencia/AdministracionDbContext.cs` y `Inf/Persistencia/Configuraciones/EventoSeguridadConfiguracion.cs`
- [X] T017 Implementar `RepositorioEventosSeguridadEfCore` con conexión y transacción propias, para que el evento quede guardado aunque la operación que lo causó se revierta (research D3), en `Inf/Persistencia/RepositorioEventosSeguridadEfCore.cs`
- [X] T018 [P] Implementar el doble `RepositorioEventosSeguridadEnMemoria` en `Inf/Persistencia/RepositorioEventosSeguridadEnMemoria.cs` (docs §2: los dobles en memoria viven en Infraestructura)
- [X] T019 Crear la migración inicial `Inicial`: esquema `administracion`, tabla `eventos_seguridad` con índice `(fecha_hora DESC)`, función `administracion.bloquear_modificacion()`, *trigger* `BEFORE UPDATE OR DELETE` sobre `eventos_seguridad` y `REVOKE UPDATE, DELETE` (research D5), en `Inf/Persistencia/Migraciones/`
- [X] T020 Crear `AgregarModuloAdministracion(IServiceCollection, IConfiguration)` que registra `ConexionOltp`, `IUnidadDeTrabajo`, `IReloj`, `AdministracionDbContext` y los repositorios, en `Inf/DependencyInjection.cs` (principio I, docs §6.5)

### Host Sigh.Api

- [X] T021 Configurar la autenticación JWT HS256: clave desde `Jwt__ClaveFirma` (la API no arranca si falta o tiene menos de 32 bytes), emisor y audiencia desde configuración, `ClockSkew` de 30 s, en `Api/Configuracion/ConfiguracionJwt.cs` (research D4, ADR-012)
- [X] T022 Configurar la autorización: una política por cada `CodigoPermiso` que exige el *claim* `perm`; política de respaldo que exige usuario autenticado; requisito global `RequisitoSinCambioPendiente` que responde `403` si el token tiene `cambio_contrasena_pendiente`, salvo en endpoints marcados con `PermitirConCambioContrasenaPendienteAttribute`; en `Api/Autorizacion/ConfiguracionAutorizacion.cs` y `Api/Autorizacion/RequisitoSinCambioPendiente.cs` (research D4, FR-006, FR-007)
- [X] T023 Crear el manejador global de errores que traduce `ReglaNegocioError` → `422`, `RecursoNoEncontradoError` → `404`, `ConflictoError` → `409` y cualquier otro error → `500`, siempre en `application/problem+json` con `traceId` y sin trazas internas; y respuestas `401`/`403` también en `problem+json`; en `Api/Middlewares/ManejadorErrores.cs` (principio III, contrato `Problema`)
- [X] T024 Crear el *health check* `/salud` que abre la conexión a `sigh_oltp`, en `Api/Salud/VerificacionBaseDatos.cs` (quickstart paso 3)
- [X] T025 Crear la aplicación de migraciones al arrancar en `Api/Arranque/AplicarMigraciones.cs`
- [X] T026 Escribir `Api/Program.cs`: `AgregarModuloAdministracion`, JWT, autorización, manejador de errores, CORS limitado a `Cors__OrigenPermitido`, controladores con prefijo `/api/v1`, `/salud`, migraciones al arrancar, y registro de `AdmApi` como *application part* (docs §6.5, principio III)

### Soporte de pruebas

- [X] T027 [P] Crear el doble `RelojFijo` en `TApl/Dobles/RelojFijo.cs` y una copia equivalente en `TDom/Dobles/RelojFijo.cs`
- [X] T028 [P] Prueba de que `MatrizPermisos` coincide fila por fila con la tabla de spec.md y de que `PermisosDe` devuelve la unión para dos roles, en `TDom/Permisos/MatrizPermisosTests.cs`
- [X] T029 [P] Prueba de `EventoSeguridad`: rechaza `Detalle` mayor a 500 caracteres y toma la hora de `IReloj`, en `TDom/Seguridad/EventoSeguridadTests.cs`
- [X] T030 [P] Prueba de arquitectura: los ensamblados `Sigh.Administracion.Dominio` y `Sigh.Compartido` (salvo `Persistencia/`) no referencian EF Core, Npgsql ni ASP.NET Core, en `TDom/Arquitectura/ReglaDependenciaTests.cs` (principio I)
- [X] T031 Crear `FabricaApiPruebas` (`WebApplicationFactory<Program>`) que reemplaza los repositorios por los dobles en memoria, usa una clave JWT de prueba y ofrece un emisor de tokens de prueba por rol, expirados, con firma inválida o con cambio pendiente, en `TApi/Soporte/FabricaApiPruebas.cs` y `TApi/Soporte/TokensPrueba.cs` (research D10)
- [X] T032 Crear el *fixture* `PostgresFixture` con Testcontainers (`postgres:17`) que aplica las migraciones, en `TInt/Soporte/PostgresFixture.cs`
- [X] T033 Prueba de integración: `UPDATE` y `DELETE` sobre `administracion.eventos_seguridad` fallan por el *trigger*, en `TInt/Persistencia/EventosSeguridadInmutablesTests.cs` (FR-021, SC-003)

### Frontend base

- [X] T034 [P] Crear el cliente HTTP que agrega `Authorization: Bearer`, interpreta `problem+json` y ante `401` borra la sesión y lleva al inicio de sesión, en `front/src/api/cliente.ts` (research D9)
- [X] T035 Crear el contexto de sesión (token, usuario, permisos, `expiraEn` y cambio pendiente, guardados en `sessionStorage`) y el componente `RutaProtegida` que exige sesión y, opcionalmente, un permiso, en `front/src/auth/ContextoSesion.tsx` y `front/src/auth/RutaProtegida.tsx`
- [X] T036 Crear el enrutador y el menú construido con los permisos de la sesión, en `front/src/App.tsx` y `front/src/components/MenuPrincipal.tsx`

### Contenedores

- [X] T037 [P] Crear `back/Dockerfile` (build con SDK 10 y ejecución con `aspnet:10`, puerto 8080)
- [X] T038 [P] Crear `front/Dockerfile` (build con Node 22 y servido con nginx) y `front/nginx.conf` con *fallback* a `index.html`
- [X] T039 Crear `tecnologia/docker-compose.yml` con los servicios `postgres` (17), `api` y `web`, todos con *healthcheck* y `restart: always`, leyendo variables de `../.env` (principio XI, quickstart paso 3)

**Punto de control**: la solución compila, `docker compose up` levanta los tres servicios, `/salud`
responde y las pruebas de la fase pasan.

---

## Fase 3: Historia 1 — Iniciar sesión en el sistema (P1) 🎯 MVP

**Objetivo**: inicio de sesión con JWT de 30 minutos, bloqueo tras 5 intentos fallidos, mensaje
genérico, cambio de contraseña (obligatorio tras una contraseña asignada), cierre de sesión y
administrador inicial desde el entorno (RF21, FR-001 a FR-007, FR-016, FR-026).

**Prueba independiente**: con usuarios ficticios precargados, iniciar sesión con credenciales válidas
e inválidas y verificar que solo las válidas obtienen una sesión con acceso a funciones protegidas.

### Pruebas de la historia 1 ⚠️

- [X] T040 [P] [US1] Pruebas de `Usuario.Crear` (normalización y validación de [data-model.md](./data-model.md) `Usuario.NombreUsuario`, `Usuario.NombreCompleto`; al menos un rol en `Usuario.Roles`; estado inicial de `Usuario.Estado` y `Usuario.CambioContrasenaPendiente`) y de `Usuario.IntentarAutenticar` (desactivado, bloqueado, quinto fallo fija `Usuario.BloqueadoHasta`, éxito reinicia `Usuario.IntentosFallidos` y actualiza `Usuario.UltimoAcceso`) en `TDom/Usuarios/UsuarioAutenticacionTests.cs`
- [X] T041 [P] [US1] Pruebas de `ContrasenaPropuesta` (8 o más caracteres, con letras y números) y de `Usuario.CambiarContrasena` en `TDom/Usuarios/ContrasenaPropuestaTests.cs`
- [X] T042 [P] [US1] Pruebas de `IniciarSesionCasoUso`: resultado genérico para contraseña incorrecta, usuario inexistente (calcula hash de referencia, `ActorId` nulo), desactivado y bloqueado; eventos `INICIO_SESION_EXITOSO`, `INICIO_SESION_FALLIDO` y `CUENTA_BLOQUEADA` sin contraseñas; permisos efectivos calculados con `MatrizPermisos`; en `TApl/Autenticacion/IniciarSesionCasoUsoTests.cs`
- [X] T043 [P] [US1] Pruebas de `CambiarContrasenaCasoUso` (exige la actual, aplica la política, quita el cambio pendiente, entrega sesión nueva y registra `CONTRASENA_CAMBIADA`) en `TApl/Autenticacion/CambiarContrasenaCasoUsoTests.cs`
- [X] T044 [P] [US1] Pruebas de `CrearAdministradorInicialCasoUso`: sin usuarios y con las tres variables crea un `Administrador` con cambio pendiente; si falta una variable no crea nada y avisa sin mostrar valores; si ya hay usuarios no hace nada; en `TApl/AdministradorInicial/CrearAdministradorInicialCasoUsoTests.cs` (FR-016)
- [X] T045 [P] [US1] Pruebas de API de `AutenticacionController`: `200` con token cuyos *claims* son `sub`, `name`, `role`, `perm` y `exp` a 30 minutos; `401` con el mismo cuerpo "Usuario o contraseña incorrectos" para los cuatro casos de FR-003/FR-004; `400` por validación; en `TApi/Autenticacion/IniciarSesionTests.cs`
- [X] T046 [P] [US1] Pruebas de API de sesión: sin token, con firma inválida y con token vencido → `401`; con cambio pendiente → `403` en cualquier endpoint salvo cambiar contraseña y cerrar sesión; en `TApi/Autenticacion/SesionTests.cs` (FR-002, FR-006, FR-007, SC-001)
- [X] T047 [P] [US1] Prueba de integración del repositorio de usuarios: `UNIQUE (nombre_usuario)`, `CHECK` de `estado`, `CHECK` de rol en `usuario_roles`, `ON DELETE RESTRICT` y *token* `version`, según [data-model.md](./data-model.md) tablas `usuarios` y `usuario_roles`, en `TInt/Persistencia/RepositorioUsuariosTests.cs`
- [X] T048 [P] [US1] Pruebas de las pantallas de inicio de sesión (éxito, error genérico, redirección obligatoria al cambio de contraseña) y de `RutaProtegida` en `front/src/pages/IniciarSesion.test.tsx` y `front/src/auth/RutaProtegida.test.tsx`

### Implementación de la historia 1

- [X] T049 [P] [US1] Crear el objeto de valor `ContrasenaPropuesta` (política de FR-005) en `Dom/Usuarios/ContrasenaPropuesta.cs`
- [X] T050 [P] [US1] Crear los puertos `IHasherContrasenas` (`Calcular`, `Verificar`) y `IEmisorTokens` en `Dom/Contratos/IHasherContrasenas.cs` y `Dom/Contratos/IEmisorTokens.cs` (research D2, D4)
- [X] T051 [US1] Crear la raíz de agregado `Usuario` con `Crear`, `IntentarAutenticar` (devuelve `ResultadoAutenticacion` con motivo interno), `CambiarContrasena` y `EstadoUsuario`, según [data-model.md](./data-model.md) `Usuario.Id`, `Usuario.NombreUsuario`, `Usuario.NombreCompleto`, `Usuario.HashContrasena`, `Usuario.Estado`, `Usuario.CambioContrasenaPendiente`, `Usuario.IntentosFallidos`, `Usuario.BloqueadoHasta`, `Usuario.Roles`, `Usuario.CreadoEn` y `Usuario.UltimoAcceso`, en `Dom/Usuarios/Usuario.cs`, `Dom/Usuarios/EstadoUsuario.cs` y `Dom/Usuarios/ResultadoAutenticacion.cs`
- [X] T052 [US1] Crear el puerto `IRepositorioUsuarios` (`ObtenerPorIdAsync`, `ObtenerPorNombreUsuarioAsync`, `ExisteAlgunoAsync`, `AgregarAsync`, `ActualizarAsync`) en `Dom/Contratos/IRepositorioUsuarios.cs`
- [X] T053 [P] [US1] Implementar `HasherContrasenasIdentity` con `PasswordHasher<T>` en `Inf/Seguridad/HasherContrasenasIdentity.cs` (research D2)
- [X] T054 [P] [US1] Implementar `EmisorTokensJwt` (HS256, 30 minutos; *claims* `sub`, `name`, `role`, `perm` y `cambio_contrasena_pendiente`) en `Inf/Seguridad/EmisorTokensJwt.cs` (research D4)
- [X] T055 [US1] Crear el mapeo `UsuarioConfiguracion` (tablas `usuarios` y `usuario_roles`, *token* `version`, `CHECK`, `ON DELETE RESTRICT`) según [data-model.md](./data-model.md) tablas `usuarios` y `usuario_roles`, en `Inf/Persistencia/Configuraciones/UsuarioConfiguracion.cs`
- [X] T056 [US1] Implementar `RepositorioUsuariosEfCore` y el doble `RepositorioUsuariosEnMemoria` en `Inf/Persistencia/RepositorioUsuariosEfCore.cs` y `Inf/Persistencia/RepositorioUsuariosEnMemoria.cs`
- [X] T057 [US1] Crear la migración `AgregarUsuarios` en `Inf/Persistencia/Migraciones/`
- [X] T058 [US1] Implementar `IniciarSesionCasoUso` con su comando y `SesionResultado` (incluye hash de referencia para usuarios inexistentes, research D6) en `Apl/Autenticacion/IniciarSesion/`
- [X] T059 [US1] Implementar `CambiarContrasenaCasoUso` y su comando en `Apl/Autenticacion/CambiarContrasena/`
- [X] T060 [US1] Implementar `CrearAdministradorInicialCasoUso` en `Apl/AdministradorInicial/CrearAdministradorInicialCasoUso.cs` y su invocación al arrancar (después de las migraciones, leyendo `AdminInicial__*`) en `Api/Arranque/CrearAdministradorInicial.cs` (research D7)
- [X] T061 [US1] Crear `AutenticacionController` con `POST iniciar-sesion` (anónimo), `POST cambiar-contrasena` y `POST cerrar-sesion` (ambos con `PermitirConCambioContrasenaPendienteAttribute`) y las solicitudes `IniciarSesionSolicitud` y `CambiarContrasenaSolicitud`, según el contrato, en `AdmApi/Controladores/AutenticacionController.cs` y `AdmApi/Solicitudes/`
- [X] T062 [US1] Registrar hasher, emisor, repositorio de usuarios en `Inf/DependencyInjection.cs` y los casos de uso de la historia en `Api/Program.cs`
- [X] T063 [P] [US1] Crear la pantalla de inicio de sesión en `front/src/pages/IniciarSesion.tsx` y el botón de cerrar sesión en `front/src/components/MenuPrincipal.tsx`
- [X] T064 [US1] Crear la pantalla de cambio de contraseña y la redirección obligatoria cuando `cambioContrasenaPendiente` es verdadero, en `front/src/pages/CambiarContrasena.tsx` y `front/src/auth/RutaProtegida.tsx`

**Punto de control**: el administrador inicial entra, cambia su contraseña y obtiene una sesión
válida; las credenciales inválidas reciben el mensaje genérico y el bloqueo funciona.

---

## Fase 4: Historia 2 — Gestionar usuarios y asignar roles (P1)

**Objetivo**: el administrador registra, lista, consulta, edita, desactiva, reactiva usuarios,
cambia sus roles y restablece su contraseña, sin eliminación y sin quedarse sin administradores
(HU14, RF22, FR-008 a FR-010, FR-014, FR-015, FR-017).

**Prueba independiente**: el administrador crea un usuario Farmacéutico; ese usuario inicia sesión y
recibe `403` en las funciones de gestión de usuarios.

### Pruebas de la historia 2 ⚠️

- [X] T065 [P] [US2] Pruebas de `Usuario.AsignarRoles` (al menos un rol), `EditarDatos`, `Desactivar`, `Reactivar` y `RestablecerContrasena` (cambio pendiente y reinicio del bloqueo) en `TDom/Usuarios/UsuarioGestionTests.cs`
- [X] T066 [P] [US2] Pruebas de `PoliticaAdministradores`: último administrador activo, auto-desactivación y auto-retiro del rol `Administrador` en `TDom/Usuarios/PoliticaAdministradoresTests.cs` (FR-014)
- [X] T067 [P] [US2] Pruebas de los casos de uso de gestión: crear (duplicado normalizado → `ConflictoError`), editar, cambiar roles, desactivar, reactivar, restablecer contraseña, listar (búsqueda, filtro por rol y estado, paginación) y consultar; cada acción registra su evento (`USUARIO_CREADO`, `USUARIO_EDITADO`, `ROLES_CAMBIADOS` con detalle `roles: A → B`, `USUARIO_DESACTIVADO`, `USUARIO_REACTIVADO`, `CONTRASENA_RESTABLECIDA`) sin contraseñas en `EventoSeguridad.Detalle`; en `TApl/Usuarios/GestionUsuariosTests.cs` (FR-017)
- [X] T068 [P] [US2] Pruebas de API de `UsuariosController`: `201` con `Location`, `409`, `422` (último administrador, auto-desactivación), `404`, ausencia de `DELETE` (`405`), respuesta sin hash ni contraseñas, y `403` para un usuario sin `usuarios.gestionar`; en `TApi/Usuarios/UsuariosControllerTests.cs`
- [X] T069 [P] [US2] Prueba de la pantalla de usuarios (listar, crear con roles, desactivar) en `front/src/pages/Usuarios.test.tsx`

### Implementación de la historia 2

- [X] T070 [US2] Agregar a `Usuario` los métodos `EditarDatos`, `AsignarRoles`, `Desactivar`, `Reactivar` y `RestablecerContrasena`, según [data-model.md](./data-model.md) `Usuario.Roles`, `Usuario.Estado`, `Usuario.CambioContrasenaPendiente` y `Usuario.BloqueadoHasta`, en `Dom/Usuarios/Usuario.cs`
- [X] T071 [US2] Crear el servicio de dominio `PoliticaAdministradores` en `Dom/Usuarios/PoliticaAdministradores.cs`
- [X] T072 [US2] Ampliar `IRepositorioUsuarios` con `ContarAdministradoresActivosAsync` y `ListarAsync(filtro, pagina, tamano)` en `Dom/Contratos/IRepositorioUsuarios.cs`, y sus implementaciones en `Inf/Persistencia/RepositorioUsuariosEfCore.cs` y `Inf/Persistencia/RepositorioUsuariosEnMemoria.cs`
- [X] T073 [US2] Crear el DTO `UsuarioDto` (sin hash) en `Apl/Usuarios/UsuarioDto.cs`
- [X] T074 [US2] Implementar los casos de uso `CrearUsuario`, `EditarUsuario`, `CambiarRoles`, `DesactivarUsuario`, `ReactivarUsuario`, `RestablecerContrasena`, `ListarUsuarios` y `ConsultarUsuario`, cada uno en su carpeta bajo `Apl/Usuarios/`; cada acción registra su evento de seguridad, que se guarda en su propia transacción (research D3)
- [X] T075 [US2] Crear `UsuariosController` con `[Authorize(Policy = CodigoPermiso.GestionarUsuarios)]`, las rutas del contrato (sin `DELETE`) y las solicitudes `CrearUsuarioSolicitud`, `EditarUsuarioSolicitud`, `CambiarRolesSolicitud` y `RestablecerContrasenaSolicitud`, en `AdmApi/Controladores/UsuariosController.cs` y `AdmApi/Solicitudes/`
- [X] T076 [US2] Registrar los casos de uso de la historia en `Api/Program.cs`
- [X] T077 [US2] Crear la pantalla de usuarios (lista con búsqueda y filtros, formulario de alta y edición con roles, desactivar, reactivar y restablecer contraseña) en `front/src/pages/Usuarios.tsx` y `front/src/components/FormularioUsuario.tsx`

**Punto de control**: las historias 1 y 2 funcionan juntas; un usuario creado por el administrador
inicia sesión con sus roles.

---

## Fase 5: Historia 3 — Aplicar permisos por rol en todo el sistema (P1)

**Objetivo**: cada endpoint exige su permiso, los rechazos quedan como evento `ACCESO_DENEGADO` y el
administrador consulta la matriz fija (RF22, ADR-007, EQ-04, FR-012, FR-013).

**Prueba independiente**: para cada rol, ejecutar una función permitida y otra no permitida, y
verificar que el 100 % de las no permitidas se rechaza (SC-001).

### Pruebas de la historia 3 ⚠️

- [X] T078 [P] [US3] Crear un controlador solo de pruebas con un endpoint por cada `CodigoPermiso` en `TApi/Soporte/ControladorPermisosPrueba.cs`, y la prueba que recorre los 8 roles × 13 permisos: `200` si `MatrizPermisos` lo concede y `403` si no, más un usuario con dos roles, en `TApi/Autorizacion/AutorizacionPorPermisoTests.cs` (SC-001)
- [X] T079 [P] [US3] Prueba de que un `403` no expone información en el cuerpo y registra `ACCESO_DENEGADO` con actor y ruta, en `TApi/Autorizacion/AccesoDenegadoTests.cs` (historia 2, escenario 6; historia 3, escenario 2)
- [X] T080 [P] [US3] Pruebas de `RolesController`: `GET /api/v1/roles/matriz-permisos` devuelve 8 roles y 13 permisos iguales a `MatrizPermisos`; no existen rutas para crear o editar roles ni permisos; en `TApi/Permisos/MatrizPermisosControllerTests.cs`
- [X] T081 [P] [US3] Prueba de integración: las tablas `roles`, `permisos` y `rol_permisos` coinciden con `MatrizPermisos`, en `TInt/Persistencia/CatalogoPermisosTests.cs` (data-model §2)
- [X] T082 [P] [US3] Prueba de la pantalla de la matriz (solo lectura, sin botones de edición) en `front/src/pages/MatrizPermisos.test.tsx`

### Implementación de la historia 3

- [X] T083 [P] [US3] Crear los nombres y descripciones visibles de cada rol en `Dom/Roles/CatalogoRoles.cs`
- [X] T084 [US3] Implementar `RegistrarAccesoDenegadoCasoUso` en `Apl/Seguridad/RegistrarAccesoDenegado/` y el manejador `IAuthorizationMiddlewareResultHandler` que lo invoca en cada `403` en `Api/Autorizacion/ManejadorResultadoAutorizacion.cs`
- [X] T085 [US3] Implementar `ConsultarMatrizPermisosCasoUso` y su DTO en `Apl/Permisos/ConsultarMatrizPermisos/`
- [X] T086 [US3] Crear `RolesController` (solo `GET matriz-permisos`, permiso `usuarios.gestionar`) en `AdmApi/Controladores/RolesController.cs`
- [X] T087 [US3] Crear la migración `AgregarCatalogoRolesPermisos` con los datos de `CatalogoRoles`, `CodigoPermiso` y `MatrizPermisos` en `Inf/Persistencia/Migraciones/` y sus mapeos en `Inf/Persistencia/Configuraciones/CatalogoPermisosConfiguracion.cs`
- [X] T088 [US3] Registrar los casos de uso y el manejador en `Api/Program.cs`
- [X] T089 [US3] Crear la pantalla de solo lectura de la matriz en `front/src/pages/MatrizPermisos.tsx`

**Punto de control**: el 100 % de las combinaciones rol × permiso se comporta según la matriz.

---

## Fase 6: Historia 4 — Registrar los accesos a las historias clínicas (P1)

**Objetivo**: servicio público `IServicioBitacora` para los demás módulos, con registro en la misma
transacción que la operación clínica (FR-022), registro de intentos denegados en transacción propia
(FR-020) e inmutabilidad en base de datos (FR-018 a FR-023, RN-06, RF24).

**Prueba independiente**: desde un módulo simulado, registrar un acceso permitido y uno denegado a
una historia ficticia y verificar que ambos quedan con todos sus datos.

### Pruebas de la historia 4 ⚠️

- [X] T090 [P] [US4] Pruebas de `EntradaBitacora`: rechaza `Accion` y `ModuloOrigen` fuera de la lista cerrada y no tiene métodos de modificación, según [data-model.md](./data-model.md) `EntradaBitacora.Accion`, `EntradaBitacora.ModuloOrigen`, `EntradaBitacora.Resultado` y `EntradaBitacora.Rol`, en `TDom/Bitacora/EntradaBitacoraTests.cs`
- [X] T091 [P] [US4] Pruebas de `ServicioBitacora`: `RegistrarAccesoAsync` sin transacción activa lanza `InvalidOperationException`; completa `NombreUsuario` y `FechaHora` dentro del módulo; valores fuera de lista → `ArgumentException`; `RegistrarIntentoDenegadoAsync` guarda `Denegado`; en `TApl/Bitacora/ServicioBitacoraTests.cs` (contrato servicio-bitacora.md)
- [X] T092 [P] [US4] Prueba de integración con un "módulo simulado" (tabla propia en el esquema `modulo_simulado`): ambas escrituras se confirman juntas; si falla la bitácora, la del módulo se revierte; un intento denegado queda aunque se revierta la operación; en `TInt/Bitacora/TransaccionCompartidaTests.cs` (FR-022, SC-002)
- [X] T093 [P] [US4] Prueba de integración: `UPDATE` y `DELETE` sobre `administracion.bitacora_accesos_hce` fallan por el *trigger*, en `TInt/Bitacora/BitacoraInmutableTests.cs` (FR-021, SC-003)
- [X] T094 [P] [US4] Prueba de integración: 100 registros simultáneos quedan todos guardados, en `TInt/Bitacora/RegistrosConcurrentesTests.cs` (caso límite "muchos accesos simultáneos")

### Implementación de la historia 4

- [X] T095 [P] [US4] Crear la entidad inmutable `EntradaBitacora` y las listas cerradas `AccionBitacora`, `ModuloOrigen` y `ResultadoAcceso`, según [data-model.md](./data-model.md) `EntradaBitacora.Id`, `EntradaBitacora.UsuarioId`, `EntradaBitacora.NombreUsuario`, `EntradaBitacora.Rol`, `EntradaBitacora.FechaHora`, `EntradaBitacora.Accion`, `EntradaBitacora.HistoriaId`, `EntradaBitacora.ModuloOrigen` y `EntradaBitacora.Resultado`, en `Dom/Bitacora/`
- [X] T096 [P] [US4] Crear el puerto de solo inserción `IRepositorioBitacora` (`AgregarAsync`, `AgregarEnTransaccionPropiaAsync`) en `Dom/Contratos/IRepositorioBitacora.cs`
- [X] T097 [US4] Crear el contrato público `IServicioBitacora` y `RegistroAcceso` exactamente como en [contracts/servicio-bitacora.md](./contracts/servicio-bitacora.md), en `Apl/Publico/IServicioBitacora.cs` y `Apl/Publico/RegistroAcceso.cs`
- [X] T098 [US4] Implementar `ServicioBitacora` (interno al módulo) en `Apl/Bitacora/ServicioBitacora.cs`
- [X] T099 [US4] Crear el mapeo `EntradaBitacoraConfiguracion` según [data-model.md](./data-model.md) tabla `bitacora_accesos_hce`, `RepositorioBitacoraEfCore` (la variante denegada usa conexión y transacción propias, research D3) y el doble `RepositorioBitacoraEnMemoria`, en `Inf/Persistencia/Configuraciones/EntradaBitacoraConfiguracion.cs`, `Inf/Persistencia/RepositorioBitacoraEfCore.cs` y `Inf/Persistencia/RepositorioBitacoraEnMemoria.cs`
- [X] T100 [US4] Crear la migración `AgregarBitacoraAccesosHce` con los tres índices, el *trigger* `bloquear_modificacion` y `REVOKE UPDATE, DELETE`, en `Inf/Persistencia/Migraciones/`
- [X] T101 [US4] Registrar `IServicioBitacora` y el repositorio en `Inf/DependencyInjection.cs`

**Punto de control**: la bitácora registra accesos permitidos y denegados, y nadie puede alterarla.

---

## Fase 7: Historia 5 — Consultar la bitácora de accesos (P2)

**Objetivo**: el administrador consulta la bitácora con filtros, paginación y orden descendente; cada
consulta queda como evento `BITACORA_CONSULTADA` (HU16, FR-024, FR-025).

**Prueba independiente**: con entradas ficticias precargadas, filtrar por usuario y rango de fechas y
obtener exactamente las esperadas, de la más reciente a la más antigua.

### Pruebas de la historia 5 ⚠️

- [X] T102 [P] [US5] Pruebas de `ConsultarBitacoraCasoUso`: rango obligatorio, `desde ≤ hasta`, un año como máximo y `tamano ≤ 100` (→ `ReglaNegocioError`); combinación de filtros; orden descendente; evento `BITACORA_CONSULTADA` con los filtros en `EventoSeguridad.Detalle`; en `TApl/Bitacora/ConsultarBitacoraCasoUsoTests.cs`
- [X] T103 [P] [US5] Pruebas de API de `BitacoraController`: `200` para el administrador con exactamente los campos de `EntradaBitacora` del contrato (FR-023), `403` para los demás roles, `422` por rango inválido; en `TApi/Bitacora/BitacoraControllerTests.cs`
- [X] T104 [P] [US5] Prueba de integración de rendimiento: con 100 000 entradas ficticias, el 95 % de las consultas filtradas responde en 2 s o menos, en `TInt/Bitacora/ConsultaBitacoraRendimientoTests.cs` (SC-006, research D8)
- [X] T105 [P] [US5] Prueba de la pantalla de la bitácora (filtros, paginación, mensaje de rango inválido) en `front/src/pages/Bitacora.test.tsx`

### Implementación de la historia 5

- [X] T106 [US5] Agregar `ConsultarAsync(filtro, pagina, tamano)` a `IRepositorioBitacora` en `Dom/Contratos/IRepositorioBitacora.cs`, y sus implementaciones en `Inf/Persistencia/RepositorioBitacoraEfCore.cs` y `Inf/Persistencia/RepositorioBitacoraEnMemoria.cs`
- [X] T107 [US5] Implementar `ConsultarBitacoraCasoUso`, su consulta y su DTO en `Apl/Bitacora/ConsultarBitacora/`
- [X] T108 [US5] Crear `BitacoraController` (`GET /api/v1/bitacora-accesos`, permiso `bitacora.consultar`) en `AdmApi/Controladores/BitacoraController.cs`
- [X] T109 [US5] Registrar el caso de uso en `Api/Program.cs`
- [X] T110 [US5] Crear la pantalla de la bitácora con filtros y paginación en `front/src/pages/Bitacora.tsx`

**Punto de control**: las cinco historias funcionan de forma independiente.

---

## Fase 8: Cierre y aspectos transversales

- [X] T111 [P] Prueba de integración de carga: 100 inicios de sesión simultáneos con el 95 % en 2 s o menos, en `TInt/Autenticacion/InicioSesionCargaTests.cs` (SC-007)
- [X] T112 [P] Prueba de que los registros (*logs*) de inicio de sesión, cambio y restablecimiento de contraseña no contienen la contraseña ni el token, en `TApi/Seguridad/SinSecretosEnLogsTests.cs` (FR-023, SC-008)
- [X] T113 Ejecutar `dotnet test` (con y sin `Category=Integracion`) y `npm test` en `front/`, y corregir lo que falle
- [X] T114 Ejecutar la validación manual completa de [quickstart.md](./quickstart.md) (pasos 1 a 5) con `docker compose up`

---

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Fase 1 (configuración)**: sin dependencias.
- **Fase 2 (base)**: depende de la fase 1 y **bloquea todas las historias**.
- **Historia 1**: depende de la fase 2.
- **Historia 2**: depende de la historia 1 (amplía `Usuario`, `IRepositorioUsuarios` y necesita
  iniciar sesión para probar).
- **Historia 3**: depende de la fase 2; sus pruebas de API usan tokens de prueba, así que puede ir en
  paralelo con la historia 2 una vez terminada la historia 1 (comparten `Api/Program.cs`).
- **Historia 4**: depende solo de la fase 2 (usa `IUnidadDeTrabajo`, `IReloj` y, para
  `NombreUsuario`, `IRepositorioUsuarios` de la historia 1).
- **Historia 5**: depende de la historia 4.
- **Fase 8**: depende de todas las historias.

### Dentro de cada historia

- Pruebas primero, y deben fallar.
- Dominio → puertos → infraestructura → casos de uso → controladores → frontend.
- Las tareas que modifican el mismo archivo (`Usuario.cs`, `Program.cs`, `DependencyInjection.cs`,
  `IRepositorioUsuarios.cs`) no llevan `[P]` y se hacen en orden.

### Oportunidades de paralelismo

- Fase 1: T002 a T005.
- Fase 2: T006 a T008, T010 a T012, T014, T015, T018, T027 a T030, T034, T037 y T038.
- Cada historia: todas sus pruebas marcadas `[P]`.
- Historias 3 y 4 pueden avanzar en paralelo después de la historia 1.

---

## Ejemplo de paralelismo: historia 1

```text
# Pruebas de la historia 1, juntas:
T040 Pruebas de Usuario.Crear e IntentarAutenticar en TDom/Usuarios/UsuarioAutenticacionTests.cs
T041 Pruebas de ContrasenaPropuesta en TDom/Usuarios/ContrasenaPropuestaTests.cs
T042 Pruebas de IniciarSesionCasoUso en TApl/Autenticacion/IniciarSesionCasoUsoTests.cs
T045 Pruebas de API de inicio de sesión en TApi/Autenticacion/IniciarSesionTests.cs

# Adaptadores de infraestructura, juntos:
T053 HasherContrasenasIdentity en Inf/Seguridad/HasherContrasenasIdentity.cs
T054 EmisorTokensJwt en Inf/Seguridad/EmisorTokensJwt.cs
```

---

## Estrategia de implementación

### Primero el MVP (historia 1)

1. Fase 1 y fase 2.
2. Historia 1.
3. **Detenerse y validar**: pasos 1 y 2 de la validación manual de quickstart.md.

### Entrega incremental

1. Base lista → historia 1 (MVP: inicio de sesión).
2. Historia 2 → el administrador crea usuarios reales de prueba.
3. Historia 3 → autorización verificada para toda la matriz.
4. Historia 4 → la bitácora queda lista para la HCE.
5. Historia 5 → supervisión de la bitácora.
6. Cada historia se confirma con su propio commit después de revisión.

---

## Notas

- `[P]` = archivos distintos y sin dependencias pendientes.
- No se crea ningún endpoint ni caso de uso de eliminación de usuarios (FR-009).
- Ningún secreto se escribe en código, `appsettings.json` ni pruebas: las pruebas generan sus claves
  en tiempo de ejecución (principio VI).
