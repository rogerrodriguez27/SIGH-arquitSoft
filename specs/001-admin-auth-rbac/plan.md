# Plan de implementación: Administración — autenticación, usuarios, roles y bitácora de accesos

**Rama**: `001-admin-auth-rbac` | **Fecha**: 2026-11-26 | **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de la funcionalidad en `specs/001-admin-auth-rbac/spec.md`.

## Resumen

Se construye el primer módulo del monolito modular, **Administración**, y la base técnica que usarán
los demás módulos: la solución .NET en `tecnologia/backend`, el host `Sigh.Api`, el proyecto
`Sigh.Compartido` y el entorno Docker Compose.

El módulo ofrece:

- inicio de sesión con JWT de 30 minutos, bloqueo tras 5 intentos fallidos y cambio obligatorio de
  contraseña (RF21);
- gestión de usuarios con 8 roles fijos y una matriz de permisos fija, definida en código (RF22, HU14);
- autorización por permiso en toda la API (ADR-007, EQ-04);
- una bitácora de accesos a la HCE de solo inserción y eventos de seguridad, con consulta filtrada
  para el administrador (RF24, HU16, RN-06).

Para cumplir FR-022 entre módulos sin compartir tablas, todos los `DbContext` de una petición usan la
**misma conexión y la misma transacción de PostgreSQL** (unidad de trabajo compartida en
`Sigh.Compartido`). Cada módulo sigue escribiendo solo en su propio esquema (ver
[research.md](./research.md), decisión D3).

El frontend React incluye solo las pantallas de esta funcionalidad: inicio de sesión, cambio de
contraseña, usuarios, matriz de permisos y bitácora.

## Contexto técnico

**Lenguaje/Versión**: C# 14 sobre .NET 10 (LTS) para el backend; TypeScript 5 con React 19 para el frontend.

**Dependencias principales**:

- Backend:
  - ASP.NET Core 10 (controladores)
  - Microsoft.AspNetCore.Authentication.JwtBearer
  - EF Core 10 con Npgsql.EntityFrameworkCore.PostgreSQL
  - Microsoft.Extensions.Identity.Core (solo `PasswordHasher<T>`)
- Frontend:
  - Vite
  - React Router

**Almacenamiento**: PostgreSQL 17, base `sigh_oltp`, esquema `administracion`, con migraciones de EF
Core. Redis no se usa en esta funcionalidad: los catálogos llegan en una funcionalidad posterior.

**Pruebas**:

- Backend con xUnit:
  - dominio y casos de uso con dobles en memoria;
  - API con `WebApplicationFactory`;
  - persistencia y triggers con Testcontainers (PostgreSQL).
- Frontend con Vitest y React Testing Library.

**Plataforma de destino**: contenedores Linux con Docker Compose, en local y en AWS EC2 (ADR-010).

**Tipo de proyecto**: aplicación web, con una API REST modular y una SPA.

**Metas de rendimiento**:

- 95 % de los inicios de sesión en 2 s o menos con 100 usuarios simultáneos (SC-007, EQ-01).
- 95 % de las consultas a la bitácora en 2 s o menos con 100 000 entradas (SC-006).

**Restricciones**:

- API sin estado (DA07).
- Secretos solo en variables de entorno (principio VI).
- Sin datos clínicos en la bitácora ni en los logs (FR-023).
- Bitácora inmutable, también a nivel de base de datos (FR-021).

**Escala y alcance**: hospital regional con hasta unos 300 usuarios del personal y 100 simultáneos;
5 historias de usuario y 26 requisitos funcionales.

## Verificación de la constitución

*Puerta previa a la Fase 0, revisada otra vez después de la Fase 1.*

| Principio | Cómo se cumple en este plan | Estado |
|---|---|---|
| I. Monolito modular + Clean Architecture | Un solo host, `Sigh.Api`. El módulo se divide en `Dominio`, `Aplicacion`, `Infraestructura` y `Api`. El dominio no referencia paquetes NuGet. El hash de contraseñas y la emisión de JWT quedan detrás de los puertos `IHasherContrasenas` y `IEmisorTokens`. | ✅ |
| II. Módulos aislados y fachadas | Los demás módulos solo usan `IServicioBitacora` (de `Sigh.Administracion.Aplicacion.Publico`). Esquema propio `administracion`. FR-022 se resuelve con la misma transacción, sin acceso a tablas ajenas (D3). | ✅ |
| III. API versionada, segura y auditada | Rutas bajo `/api/v1`. Política global que exige JWT, salvo `POST /api/v1/autenticacion/iniciar-sesion`. Una política por permiso. Errores 401, 403, 404 y 422 controlados. CORS limitado a `Cors__OrigenPermitido`. | ✅ |
| IV. OLTP / OLAP | Solo se usa `sigh_oltp`. No hay procesos analíticos. | ✅ (no aplica) |
| V. Registros clínicos inmutables | No se tocan registros clínicos. La bitácora sigue la misma idea de solo inserción (D5). | ✅ |
| VI. Secretos fuera del repositorio | `Jwt__ClaveFirma`, `ConnectionStrings__SighOltp`, `AdminInicial__*` y `POSTGRES_PASSWORD` solo existen en `.env`. `.env.example` se amplía con valores vacíos. `appsettings.json` no contiene secretos. | ✅ |
| VII. APIPERU | No aplica a esta funcionalidad. | — |
| VIII. Notificaciones | No aplica. La matriz ya incluye el permiso de notificaciones para todos los roles. | — |
| IX. Datos ficticios | Las semillas de prueba usan usuarios ficticios. El administrador inicial se crea desde el entorno. | ✅ |
| X. Pruebas antes de terminar | Pruebas de dominio (bloqueo, contraseña, último administrador), de casos de uso y de autorización permitida y denegada para cada permiso (SC-001), más inmutabilidad en la base de datos. | ✅ |
| XI. Código en `tecnologia/` y Docker Compose | Todo bajo `tecnologia/`. `docker compose up` levanta `postgres`, `api` y `web` con healthcheck y `restart: always`. | ✅ |
| XII. `docs/` como fuente de verdad | Dos observaciones, resueltas en este plan (ver abajo). | ✅ |

**Observaciones del principio XII (resueltas):**

1. El fragmento de `Program.cs` en `diseño-interno-de-modulos.md` dice "clave desde
   appsettings.json". Esto contradice el principio VI: la clave de firma JWT se lee de
   `Jwt__ClaveFirma`. Se corrige ese comentario en `docs/` en el mismo commit del plan.
2. El esquema de carpetas de `docs/` (`src/Sigh.Api`, `src/Compartido/Sigh.Compartido`,
   `src/Modulos/<Modulo>/Sigh.<Modulo>.{Dominio,Aplicacion,Infraestructura,Api}`) se respeta tal
   cual dentro de `tecnologia/backend/`.

**Resultado de la puerta**: aprobada, sin violaciones. La tabla de complejidad queda vacía.

**Revisión después del diseño (Fase 1)**: el modelo de datos, los contratos y la decisión D3 se
revisaron contra los principios I, II, III, VI y X. Siguen sin violaciones.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/001-admin-auth-rbac/
├── plan.md              # Este archivo
├── research.md          # Fase 0: decisiones técnicas
├── data-model.md        # Fase 1: entidades, tablas y reglas
├── quickstart.md        # Fase 1: guía para levantar y validar
├── contracts/
│   ├── api-administracion.openapi.yaml   # Contrato REST /api/v1
│   └── servicio-bitacora.md              # Contrato público para otros módulos
├── checklists/requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks)
```

### Código fuente

```text
tecnologia/
├── docker-compose.yml
├── backend/
│   ├── Sigh.slnx
│   ├── Directory.Build.props            # net10.0, nullable, warnings como errores
│   ├── Directory.Packages.props         # versiones centralizadas de NuGet
│   ├── Dockerfile
│   ├── src/
│   │   ├── Sigh.Api/                    # Host único: Program.cs, middlewares, políticas
│   │   ├── Compartido/
│   │   │   └── Sigh.Compartido/         # Errores de negocio, IReloj, unidad de trabajo, conexión compartida
│   │   └── Modulos/
│   │       └── Administracion/
│   │           ├── Sigh.Administracion.Dominio/          # Usuario, Rol, Permiso, MatrizPermisos, EntradaBitacora, EventoSeguridad, puertos
│   │           ├── Sigh.Administracion.Aplicacion/       # Casos de uso + Publico/IServicioBitacora
│   │           ├── Sigh.Administracion.Infraestructura/  # AdministracionDbContext, repositorios, migraciones, hasher, emisor JWT, DependencyInjection.cs
│   │           └── Sigh.Administracion.Api/              # Controladores y solicitudes HTTP
│   └── tests/
│       ├── Sigh.Administracion.Dominio.Tests/
│       ├── Sigh.Administracion.Aplicacion.Tests/
│       ├── Sigh.Api.Tests/                               # WebApplicationFactory: autorización por rol (SC-001)
│       └── Sigh.Administracion.Integracion.Tests/        # Testcontainers: PostgreSQL, triggers y transacción compartida
└── frontend/
    ├── Dockerfile                       # build con Node y servido con nginx
    ├── package.json
    ├── vite.config.ts
    └── src/
        ├── api/                         # cliente HTTP con JWT
        ├── auth/                        # contexto de sesión y rutas protegidas por permiso
        ├── pages/                       # IniciarSesion, CambiarContrasena, Usuarios, MatrizPermisos, Bitacora
        └── components/
```

**Decisión de estructura**:

- Backend: se sigue la estructura de `docs/03-diseño-de-software`. Se agrega el prefijo `tecnologia/`
  (principio XI) y las carpetas `backend/` y `frontend/`.
- Pruebas: en `tests/`, separadas por capa.
- Módulos futuros: se agregan como `src/Modulos/<Modulo>/` sin cambiar esta estructura.

## Seguimiento de complejidad

Sin violaciones de la constitución que justificar.
