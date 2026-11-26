# Pruebas automatizadas y cobertura (Sprint 2)

## Herramientas

El enunciado pide Jest "o lo que corresponda". Jest es para JavaScript, así que en este proyecto se
usa su equivalente en cada tecnología:

| Parte | Herramienta | Por qué |
|---|---|---|
| Backend .NET 10 | **xUnit** + WebApplicationFactory + Testcontainers | Es el marco de pruebas estándar de .NET. Testcontainers levanta PostgreSQL 17 real para los triggers y las transacciones (research D10) |
| Frontend React | **Vitest** + React Testing Library | Tiene la misma API que Jest (`describe`, `it`, `expect`, `vi.fn`) y es el marco nativo de Vite |
| Cobertura del backend | coverlet + ReportGenerator | Reporte HTML y resumen por ensamblado y clase |
| Cobertura del frontend | `@vitest/coverage-v8` | Reporte HTML, `lcov` y resumen |
| Atributos de calidad | **k6** + PowerShell | Carga, escalabilidad, seguridad y disponibilidad contra el sistema desplegado ([01-evidencias-atributos-calidad.md](01-evidencias-atributos-calidad.md)) |
| Integración continua | **GitHub Actions** | [.github/workflows/ci.yml](../../.github/workflows/ci.yml) |

## Pruebas del backend (348)

| Proyecto | Cantidad | Qué valida | ¿Necesita Docker? |
|---|---|---|---|
| `Sigh.Administracion.Dominio.Tests` | 86 | Reglas de negocio: bloqueo tras 5 fallos, política de contraseñas, último administrador, matriz fija, inmutabilidad, regla de dependencia | No |
| `Sigh.Administracion.Aplicacion.Tests` | 61 | Casos de uso con dobles en memoria, con sus caminos alternativos y de error | No |
| `Sigh.Api.Tests` | 171 | API completa: autenticación, 104 combinaciones rol × permiso (SC-001), errores `problem+json`, OpenAPI, sin secretos en los logs (SC-008) | No |
| `Sigh.Administracion.Integracion.Tests` | 30 | PostgreSQL real: migraciones, triggers, transacción compartida (FR-022), concurrencia, rendimiento (SC-006, SC-007) | Sí |

## Pruebas del frontend (27)

Cubren:

- el inicio de sesión: éxito, mensaje genérico y cambio de contraseña obligatorio;
- las rutas protegidas, incluido el caso de abrir una ruta directamente con la sesión guardada;
- el cambio de contraseña;
- la gestión de usuarios: alta, edición de roles, desactivación, restablecimiento y filtros;
- la matriz de permisos, que es de solo lectura;
- la bitácora: filtros, paginación y rango inválido.

## Cobertura medida

**Backend** (excluye las pruebas y las migraciones generadas):

| Métrica | Valor |
|---|---|
| Líneas | **95,9 %** (1274 de 1328) |
| Ramas | **87,7 %** (250 de 285) |
| Métodos | **93,1 %** (219 de 235) |

| Ensamblado | Líneas |
|---|---|
| Sigh.Administracion.Dominio | 98,3 % |
| Sigh.Administracion.Aplicacion | 97,9 % |
| Sigh.Administracion.Infraestructura | 96,9 % |
| Sigh.Administracion.Api | 96,3 % |
| Sigh.Compartido | 90,6 % |
| Sigh.Api | 89,3 % |

**Frontend:**

| Métrica | Valor |
|---|---|
| Líneas | **92,9 %** (290 de 312) |
| Sentencias | 90,7 % |
| Ramas | 79,7 % |
| Funciones | 83,9 % |

Detalle por clase y archivo: [resultados/cobertura-backend.txt](resultados/cobertura-backend.txt) y
[resultados/cobertura-frontend.txt](resultados/cobertura-frontend.txt). El reporte HTML se publica como
artefacto en cada ejecución de la CI.

## Cómo ejecutarlas

```powershell
cd tecnologia/backend
dotnet test --settings cobertura.runsettings --collect "XPlat Code Coverage"
dotnet tool run reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:cobertura -reporttypes:"Html;TextSummary"
```

Abre `tecnologia/backend/cobertura/index.html`.

```powershell
cd tecnologia/frontend
npm run test:cobertura
```

Abre `tecnologia/frontend/coverage/index.html`. Para omitir las pruebas que necesitan Docker, agrega
`--filter "Category!=Integracion"` a `dotnet test`.

## Integración continua (GitHub Actions)

[.github/workflows/ci.yml](../../.github/workflows/ci.yml) se ejecuta en cada push a `main` y en cada
pull request:

| Trabajo | Qué hace |
|---|---|
| `backend` | Compila sin advertencias, ejecuta las 348 pruebas (incluidas las de integración con el Docker del runner), genera el reporte de cobertura, lo publica en el resumen de la ejecución y lo sube como artefacto |
| `frontend` | `npm ci`, las pruebas con cobertura (publicada en el resumen), y la compilación de TypeScript y Vite |
| `secretos` | gitleaks sobre todo el historial (EQ-11) |

Es el único archivo fuera de `tecnologia/`, porque GitHub solo lee los flujos desde
`.github/workflows` (excepción registrada en `plan.md`).
