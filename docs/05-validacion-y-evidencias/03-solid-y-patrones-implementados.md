# SOLID, Clean Architecture y patrones en el código implementado (Sprint 2)

## Objetivo

Mostrar **en el código real** del módulo Administración cómo se aplican el estilo, el enfoque, los
principios y los patrones definidos en:

- [estilo-arquitectonico.md](../02-arquitectura-software/estilo-arquitectonico.md)
- [enfoque-arquitectonico.md](../02-arquitectura-software/enfoque-arquitectonico.md)
- [principios-de-diseño.md](../03-diseño-de-software/diseño-interno/principios-de-diseño.md)
- [patrones-de-diseño.md](../03-diseño-de-software/diseño-interno/patrones-de-diseño.md)

Rutas abreviadas: `Dom/`, `Apl/`, `Inf/` y `AdmApi/` son las carpetas
`tecnologia/backend/src/Modulos/Administracion/Sigh.Administracion.{Dominio,Aplicacion,Infraestructura,Api}/`.

## 1. Estilo: monolito modular (ADR-001)

| Regla | Dónde se ve |
|---|---|
| Un solo desplegable | `Sigh.Api` es el único host. `Program.cs` registra cada módulo con `AgregarModuloAdministracion(...)` |
| Módulos aislados | Esquema propio `administracion`. Otros módulos solo pueden usar `Apl/Publico/IServicioBitacora.cs` |
| Comunicación entre módulos sin compartir tablas | La HCE registrará sus accesos llamando a `IServicioBitacora`, en la misma transacción gracias a `IUnidadDeTrabajo` (research D3) |

## 2. Enfoque: Clean Architecture (ADR-002)

| Capa | Proyecto | Contiene | Depende de |
|---|---|---|---|
| Dominio | `Sigh.Administracion.Dominio` | `Usuario`, `EntradaBitacora`, `EventoSeguridad`, `MatrizPermisos`, `ContrasenaPropuesta`, `PoliticaAdministradores` y los puertos `IRepositorio*`, `IHasherContrasenas`, `IEmisorTokens` | Solo `Sigh.Compartido` |
| Aplicación | `Sigh.Administracion.Aplicacion` | Un caso de uso por carpeta (`IniciarSesion`, `CrearUsuario`, `ConsultarBitacora`…) y el contrato público `IServicioBitacora` | Dominio |
| Infraestructura | `Sigh.Administracion.Infraestructura` | EF Core, repositorios, migraciones, `HasherContrasenasIdentity`, `EmisorTokensJwt`, `DependencyInjection.cs` | Dominio, Aplicación y paquetes externos |
| Presentación | `Sigh.Administracion.Api` | Controladores y solicitudes HTTP | Aplicación |
| Composición | `Sigh.Api` | `Program.cs`, autenticación, autorización, errores, OpenAPI | Todos |

**Verificación automática:** `tests/Sigh.Administracion.Dominio.Tests/Arquitectura/ReglaDependenciaTests.cs`
falla si el dominio referencia EF Core, Npgsql, ASP.NET Core o cualquier otro proyecto que no sea
`Sigh.Compartido`.

## 3. SOLID en el código

| Principio | Cómo se cumple | Archivos |
|---|---|---|
| **S** · Responsabilidad única | El controlador solo traduce HTTP. El caso de uso coordina. La entidad aplica las reglas, como el bloqueo tras 5 fallos. El repositorio solo persiste | `AdmApi/Controladores/AutenticacionController.cs`, `Apl/Autenticacion/IniciarSesion/IniciarSesionCasoUso.cs`, `Dom/Usuarios/Usuario.cs`, `Inf/Persistencia/RepositorioUsuariosEfCore.cs` |
| **O** · Abierto/cerrado | Para cambiar cómo se guarda o se firma, se agrega un adaptador nuevo sin tocar los casos de uso. Ejemplo: el repositorio en memoria de las pruebas, o `HasherConMemoria` envolviendo al hasher | `Inf/Persistencia/RepositorioUsuariosEnMemoria.cs`, `Apl/Autenticacion/IniciarSesion/IniciarSesionCasoUso.cs` |
| **L** · Sustitución de Liskov | `RepositorioUsuariosEfCore` y `RepositorioUsuariosEnMemoria` cumplen el mismo contrato, incluida la unicidad del nombre. Los casos de uso pasan las mismas pruebas con cualquiera de los dos | `Inf/Persistencia/RepositorioUsuarios*.cs`, `tests/Sigh.Administracion.Integracion.Tests/Persistencia/RepositorioUsuariosTests.cs` |
| **I** · Segregación de interfaces | Cada puerto tiene solo lo que su caso de uso necesita. `IRepositorioEventosSeguridad` y `IRepositorioBitacora` no tienen métodos de modificación ni de borrado (FR-021). `IServicioBitacora` expone solo 2 operaciones a los demás módulos | `Dom/Contratos/*.cs`, `Apl/Publico/IServicioBitacora.cs` |
| **D** · Inversión de dependencias | Los casos de uso reciben interfaces por el constructor. Las implementaciones concretas se eligen en `DependencyInjection.cs` y `Program.cs` | `Inf/DependencyInjection.cs`, `tecnologia/backend/src/Sigh.Api/Program.cs` |

## 4. Patrones de diseño aplicados

| Patrón | Dónde | Problema que resuelve |
|---|---|---|
| **Repository** | `IRepositorioUsuarios`, `IRepositorioBitacora`, `IRepositorioEventosSeguridad` y sus implementaciones `*EfCore` y `*EnMemoria` | Las reglas no conocen SQL. Las pruebas de dominio y de aplicación no necesitan PostgreSQL |
| **Adapter** | `HasherContrasenasIdentity` adapta `PasswordHasher<T>` al puerto `IHasherContrasenas`. `EmisorTokensJwt` adapta `JsonWebTokenHandler` a `IEmisorTokens` | El dominio no depende de ASP.NET Core Identity ni de las librerías de JWT |
| **Facade** | `IServicioBitacora` / `ServicioBitacora` | Los demás módulos registran accesos sin conocer las tablas ni el `DbContext` de Administración (FR-018) |
| **Unit of Work** | `Compartido/Persistencia/IUnidadDeTrabajo.cs`, `UnidadDeTrabajoNpgsql.cs`, `ConexionOltp.cs` | Escrituras de distintos módulos en una sola transacción (FR-022) |
| **Decorator** | `HasherConMemoria` dentro de `IniciarSesionCasoUso`. Para los catálogos con Redis está previsto `CatalogoCie10EnCache` | Agregar comportamiento sin modificar la clase original |
| **Domain Service** | `Dom/Usuarios/PoliticaAdministradores.cs` | Reglas que involucran a varios usuarios: no quedarse sin administradores activos (FR-014) |
| **Value Object** | `Dom/Usuarios/ContrasenaPropuesta.cs` | Una contraseña solo existe si cumple la política (FR-005) |
| **Aggregate Root** | `Dom/Usuarios/Usuario.cs` (con `RolAsignado`) | Los roles solo cambian a través del usuario, que valida que tenga al menos uno |
| **Factory** | `Usuario.Crear`, `EventoSeguridad.Crear`, `EntradaBitacora.Crear`, `FabricaContextoAdministracion` | Crear objetos siempre válidos y contextos con su propia conexión |
| **Policy / Strategy** (autorización) | `Sigh.Api/Autorizacion/ConfiguracionAutorizacion.cs`: una política por permiso, y `RequisitoSinCambioPendiente` | La regla de acceso de cada endpoint se declara con un atributo y no se repite en el código (ADR-007) |
| **Middleware / manejador global** | `Sigh.Api/Middlewares/ManejadorErrores.cs`, `ManejadorResultadoAutorizacion.cs` | Errores uniformes en `problem+json` y registro de cada 403 sin repetir lógica en los controladores |
| **CORS** | `Program.cs` | Solo el frontend del SIGH puede llamar a la API desde el navegador |

## 5. Calidad del diseño medida

| Indicador | Valor |
|---|---|
| Pruebas automáticas | 348 de backend y 27 de frontend |
| Cobertura de líneas | Backend 95,9 % · Frontend 92,9 % ([04-pruebas-y-cobertura.md](04-pruebas-y-cobertura.md)) |
| Dependencias prohibidas en el dominio | 0 (verificado por prueba) |
| Advertencias de compilación | 0: `TreatWarningsAsErrors=true` en `Directory.Build.props` |
