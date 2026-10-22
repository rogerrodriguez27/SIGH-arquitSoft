# Diseño interno de módulos

> **Proyecto:** SIGH · **Backend:** C# / .NET – ASP.NET Core · **Módulo de referencia:** HCE  
> **Enfoque arquitectónico:** Clean Architecture dentro de cada módulo del monolito modular  
> **Nivel C4:** 4 – Código · **Versión:** 1.0

---

## 1. Propósito

Este documento describe **cómo se organiza el código dentro de un módulo** del backend. Tiene cuatro objetivos:

- que todos los módulos (Admisión y Triaje, HCE, Farmacia, Laboratorio, Administración, y Reportes y Alertas) se construyan con la misma estructura;
- que las reglas del negocio clínico queden aisladas de la tecnología;
- que se sepa dónde va cada archivo y de qué puede depender;
- que sirva como base para redactar las especificaciones SDD de cada módulo, a partir de las cuales Claude Code genera el código.

## 2. Estructura de carpetas

```text
src/
├── Sigh.Api/                                  Host único del monolito (ASP.NET Core)
│   ├── Program.cs                             Raíz de composición
│   └── appsettings.json                       Configuración (cadenas de conexión, JWT)
├── Compartido/
│   └── Sigh.Compartido/                       Errores de negocio, utilidades comunes
└── Modulos/
    └── Hce/
        ├── Sigh.Hce.Dominio/
        │   ├── Entidades/
        │   │   ├── HistoriaClinica.cs         Raíz: agrupa las atenciones del paciente
        │   │   ├── Atencion.cs                Entidad Atencion y sus reglas
        │   │   ├── NotaCorreccion.cs          Corrección vinculada a una atención firmada
        │   │   └── Diagnostico.cs             Objeto de valor: código CIE-10 y descripción
        │   └── Contratos/
        │       ├── IRepositorioHistorias.cs   Interfaz (puerto) de persistencia
        │       ├── IBitacoraAuditoria.cs      Interfaz (puerto) de auditoría
        │       └── ICatalogoCie10.cs          Interfaz (puerto) de validación CIE-10
        ├── Sigh.Hce.Aplicacion/
        │   ├── RegistrarAtencion/
        │   │   ├── RegistrarAtencionCasoUso.cs    Caso de uso RegistrarAtencion
        │   │   └── RegistrarAtencionComando.cs    Datos de entrada del caso de uso
        │   ├── ConsultarHistoria/
        │   │   └── ConsultarHistoriaCasoUso.cs    Caso de uso ConsultarHistoria
        │   └── Fachada/
        │       ├── IHceFachada.cs             API pública del módulo para otros módulos
        │       └── HceFachada.cs              Implementación de la fachada
        ├── Sigh.Hce.Infraestructura/
        │   ├── Persistencia/
        │   │   ├── HceDbContext.cs            Contexto de EF Core (esquema hce)
        │   │   ├── RepositorioHistoriasEfCore.cs      Implementación con PostgreSQL
        │   │   └── RepositorioHistoriasEnMemoria.cs   Implementación para pruebas
        │   ├── Auditoria/
        │   │   └── BitacoraAuditoriaEfCore.cs Registro de accesos en PostgreSQL
        │   ├── Catalogos/
        │   │   ├── CatalogoCie10EfCore.cs     Consulta del catálogo en PostgreSQL
        │   │   └── CatalogoCie10EnCache.cs    Decorador con Redis
        │   └── DependencyInjection.cs         Registro de los adaptadores del módulo
        └── Sigh.Hce.Api/
            ├── Controladores/
            │   ├── AtencionesController.cs    Traduce HTTP ⇄ caso de uso
            │   └── HistoriasController.cs     Consulta de historias clínicas
            └── Solicitudes/
                └── RegistrarAtencionSolicitud.cs  Cuerpo de la petición HTTP
```

| Archivo | Capa | Responsabilidad | Referencia a |
|---|---|---|---|
| `HistoriaClinica.cs` | Dominio | Agrupar las atenciones de un paciente y garantizar que pertenezcan a él | Solo dominio |
| `Atencion.cs` | Dominio | Registrar evolución y diagnósticos, firmar y aplicar la regla de inmutabilidad | Solo dominio |
| `NotaCorreccion.cs` | Dominio | Representar una corrección con autor y fecha, sin alterar el original | Nada |
| `Diagnostico.cs` | Dominio | Representar un diagnóstico con su código CIE-10 | Nada |
| `IRepositorioHistorias.cs` | Dominio | Declarar **qué** operaciones de persistencia necesita el módulo | Entidades |
| `IBitacoraAuditoria.cs` | Dominio | Declarar **qué** necesita el módulo para registrar accesos | Nada |
| `ICatalogoCie10.cs` | Dominio | Declarar **qué** necesita el módulo para validar diagnósticos | Nada |
| `RegistrarAtencionCasoUso.cs` | Aplicación | Coordinar los pasos del registro de una atención | Dominio |
| `RegistrarAtencionComando.cs` | Aplicación | Datos de entrada validados del caso de uso | Nada |
| `IHceFachada.cs` / `HceFachada.cs` | Aplicación | Exponer solo lo que otros módulos pueden usar (recetas, órdenes, vínculo de resultados) | Dominio |
| `HceDbContext.cs` | Infraestructura | Mapear las entidades a tablas de PostgreSQL con EF Core | Dominio + EF Core |
| `RepositorioHistoriasEfCore.cs` | Infraestructura | Guardar y leer historias y atenciones en PostgreSQL | Dominio + EF Core |
| `RepositorioHistoriasEnMemoria.cs` | Infraestructura | Implementación en memoria para pruebas unitarias | Dominio |
| `BitacoraAuditoriaEfCore.cs` | Infraestructura | Registrar usuario, fecha, hora y acción de cada acceso | Dominio + EF Core |
| `CatalogoCie10EfCore.cs` | Infraestructura | Consultar códigos CIE-10 en PostgreSQL | Dominio + EF Core |
| `CatalogoCie10EnCache.cs` | Infraestructura | Consultar primero en Redis y luego en PostgreSQL (Decorator) | Dominio + StackExchange.Redis |
| `DependencyInjection.cs` | Infraestructura | Registrar qué adaptador implementa cada contrato (`AgregarModuloHce()`) | Todas las capas del módulo |
| `AtencionesController.cs` | Presentación | Leer la petición HTTP, invocar el caso de uso y responder | Aplicación |
| `RegistrarAtencionSolicitud.cs` | Presentación | Estructura del cuerpo JSON de la petición | Nada |
| `Program.cs` | Composición | Configurar el host, JWT, autorización y registrar todos los módulos | Todos los proyectos |

---

## 3. Capas del módulo y regla de dependencia

![Diagrama de paquetes del módulo HCE](../../../img/paquetes-modulo-hce.png)

**Regla de dependencia:** el código solo puede referenciar hacia el centro (dominio). El dominio no conoce ASP.NET Core, EF Core, PostgreSQL ni Redis. En .NET esta regla se refuerza con las **referencias entre proyectos**: `Sigh.Hce.Dominio` no referencia ningún otro proyecto, por lo que si se intenta usar EF Core en el dominio, el código no compila.

| Capa | Contiene | Responsabilidad | Puede depender de | **No** puede depender de |
|---|---|---|---|---|
| **Dominio** | Entidades, objetos de valor, interfaces (puertos) | Reglas del negocio clínico que serían ciertas con cualquier tecnología | Nada externo | Aplicación, infraestructura, presentación, ASP.NET Core, EF Core, Redis |
| **Aplicación** | Casos de uso, comandos, fachada del módulo | Orquestar el flujo: qué paso va primero y qué hacer si falla | Dominio | Infraestructura, presentación, ASP.NET Core, EF Core |
| **Infraestructura** | Repositorios, adaptadores, contexto de EF Core | Implementar las interfaces del dominio con una tecnología | Dominio, librerías externas | Presentación |
| **Presentación** | Controladores, solicitudes | Traducir HTTP a llamadas del caso de uso y viceversa | Aplicación | Infraestructura, base de datos |
| **Composición** | `Program.cs`, `DependencyInjection.cs` | Elegir implementaciones concretas y conectarlas | Todas | — |

> **Cómo comprobarlo en el código:** basta revisar el archivo `Sigh.Hce.Dominio.csproj`. Si aparece una referencia a `Microsoft.EntityFrameworkCore`, `Npgsql`, `StackExchange.Redis` o a otro proyecto del sistema, la regla se rompió.

### 3.1 Reglas de negocio del módulo HCE

| ID | Regla de negocio | Dónde se aplica |
|---|---|---|
| RN-01 | Toda atención debe pertenecer a una historia clínica existente. | `HistoriaClinica` |
| RN-02 | Una atención solo puede firmarse si tiene al menos un diagnóstico con un código CIE-10 válido. | `Atencion`, `ICatalogoCie10` |
| RN-03 | Solo un usuario con rol Médico puede registrar y firmar una atención. | Middleware de autorización, `Atencion` |
| RN-04 | Una atención firmada no se modifica ni se elimina. | `Atencion` |
| RN-05 | Las correcciones se registran como una nota vinculada a la atención original, con autor y fecha. | `Atencion`, `NotaCorreccion` |
| RN-06 | Todo acceso o modificación a una historia clínica se registra en la bitácora de auditoría. | `IBitacoraAuditoria` |
---

## 4. Diagrama de clases

![Diagrama de clases del módulo HCE](../../../img/clases-modulo-hce.png)

### 4.1 Notación UML utilizada

| Símbolo | Significado | Ejemplo en el diagrama |
|---|---|---|
| Línea continua con flecha abierta | Asociación: la clase guarda una referencia a la otra | `RegistrarAtencionCasoUso` → `IRepositorioHistorias` |
| Línea discontinua con flecha abierta | Dependencia: la usa o la crea, pero no la guarda | `AtencionesController` «crea» `RegistrarAtencionComando` |
| Línea discontinua con triángulo hueco | Realización: implementa una interfaz | `RepositorioHistoriasEfCore` ▷ `IRepositorioHistorias` |
| Rombo relleno | Composición: la parte no existe sin el todo | `HistoriaClinica` ◆ `Atencion` (0..*), `Atencion` ◆ `Diagnostico` (1..*) |
| Nombre en cursiva + «interfaz» | Interfaz (contrato sin implementación) | `IRepositorioHistorias`, `ICatalogoCie10` |
| «decorador» | Clase que envuelve a otra implementación de la misma interfaz | `CatalogoCie10EnCache` «envuelve» `CatalogoCie10EfCore` |
| `-` / `+` | Privado / público | `- Estado`, `+ Firmar(medicoId)` |

---

## 5. Flujo de ejecución: registrar atención

![Diagrama de secuencia de registrar atención](../../../img/secuencia-registrar-atencion.png)

| Paso | Origen → destino | Acción | Capa |
|---|---|---|---|
| 1 | Cliente web → `AtencionesController` | `POST /api/v1/pacientes/{id}/atenciones` con `evolucion`, `codigosCie10`, `tratamiento` y el token JWT; el middleware valida el rol Médico (RN-03) | Presentación |
| 2 | `AtencionesController` → `RegistrarAtencionCasoUso` | `EjecutarAsync(comando)`; el `medicoId` se toma del token, no del cuerpo de la petición | Presentación → Aplicación |
| 3 | `RegistrarAtencionCasoUso` → `IRepositorioHistorias` | `ObtenerPorPacienteAsync(pacienteId)`; consulta la historia en PostgreSQL (RN-01) | Aplicación → Dominio (interfaz) |
| 4 | `RegistrarAtencionCasoUso` → `ICatalogoCie10` | `ExisteCodigoAsync(codigo)` por cada diagnóstico; busca primero en Redis y, si no está, en PostgreSQL (RN-02) | Aplicación → Dominio (interfaz) |
| 5 | `RegistrarAtencionCasoUso` → `HistoriaClinica` | `RegistrarAtencion(...)`; se crea la atención en estado BORRADOR | Aplicación → Dominio |
| 6 | `RegistrarAtencionCasoUso` → `Atencion` | `Firmar(medicoId)`; pasa a estado FIRMADA y ya no puede modificarse (RN-02, RN-04) | Aplicación → Dominio |
| 7–8 | `RegistrarAtencionCasoUso` → `IRepositorioHistorias` → PostgreSQL | `GuardarAtencionAsync(atencion)`; `INSERT` de la atención y sus diagnósticos | Aplicación → Infraestructura → BD |
| 9–10 | `RegistrarAtencionCasoUso` → `IBitacoraAuditoria` → PostgreSQL | `RegistrarAccesoAsync(...)`; se registra quién, cuándo y qué acción realizó (RN-06) | Aplicación → Infraestructura → BD |
| 11 | `AtencionesController` → Cliente web | `201 Created` con `atencionId` | Presentación |
| 12–13 | Alternativa | Si la historia no existe (RN-01) o un código CIE-10 no es válido (RN-02): no se guarda la atención y se responde `404` o `422` | Aplicación → Presentación |
---

## 6. Código de referencia

> Fragmentos de referencia en C# / .NET para el módulo HCE. Sirven como ejemplo de estilo para las especificaciones SDD; el código definitivo se genera con Claude Code a partir de las especificaciones de cada módulo.

### 6.1 Dominio · entidades e interfaces

```csharp
// Sigh.Hce.Dominio/Entidades/Atencion.cs
using Sigh.Compartido.Errores;

namespace Sigh.Hce.Dominio.Entidades;

public enum EstadoAtencion { Borrador, Firmada }

public sealed class Atencion
{
    private readonly List<Diagnostico> _diagnosticos = new();
    private readonly List<NotaCorreccion> _notas = new();

    public Guid Id { get; private set; }
    public Guid HistoriaClinicaId { get; private set; }
    public Guid MedicoId { get; private set; }
    public string Evolucion { get; private set; } = string.Empty;
    public string Tratamiento { get; private set; } = string.Empty;
    public EstadoAtencion Estado { get; private set; }
    public DateTime? FechaFirma { get; private set; }
    public IReadOnlyList<Diagnostico> Diagnosticos => _diagnosticos;
    public IReadOnlyList<NotaCorreccion> Notas => _notas;

    private Atencion() { }                                       // requerido por EF Core

    internal static Atencion Crear(Guid historiaId, Guid medicoId, string evolucion,
        IEnumerable<Diagnostico> diagnosticos, string tratamiento)
    {
        if (string.IsNullOrWhiteSpace(evolucion))
            throw new ReglaNegocioError("La evolución es obligatoria.");

        var atencion = new Atencion
        {
            Id = Guid.NewGuid(),
            HistoriaClinicaId = historiaId,
            MedicoId = medicoId,
            Evolucion = evolucion,
            Tratamiento = tratamiento,
            Estado = EstadoAtencion.Borrador
        };
        atencion._diagnosticos.AddRange(diagnosticos);
        return atencion;
    }

    public void Firmar(Guid medicoId)
    {
        if (Estado == EstadoAtencion.Firmada)                    // RN-04
            throw new ReglaNegocioError("RN-04: la atención ya fue firmada y no puede modificarse.");
        if (medicoId != MedicoId)                                // RN-03
            throw new ReglaNegocioError("RN-03: solo el médico que registró la atención puede firmarla.");
        if (_diagnosticos.Count == 0)                            // RN-02
            throw new ReglaNegocioError("RN-02: la atención debe tener al menos un diagnóstico.");

        Estado = EstadoAtencion.Firmada;
        FechaFirma = DateTime.UtcNow;
    }

    public NotaCorreccion AgregarNotaCorreccion(Guid autorId, string texto)   // RN-05
    {
        if (Estado != EstadoAtencion.Firmada)
            throw new ReglaNegocioError("RN-05: las notas de corrección solo se agregan a atenciones firmadas.");

        var nota = new NotaCorreccion(Id, autorId, texto);       // el registro original no cambia
        _notas.Add(nota);
        return nota;
    }
}
```

```csharp
// Sigh.Hce.Dominio/Entidades/HistoriaClinica.cs
namespace Sigh.Hce.Dominio.Entidades;

public sealed class HistoriaClinica
{
    private readonly List<Atencion> _atenciones = new();

    public Guid Id { get; private set; }
    public Guid PacienteId { get; private set; }
    public IReadOnlyList<Atencion> Atenciones => _atenciones;

    private HistoriaClinica() { }                                // requerido por EF Core

    public Atencion RegistrarAtencion(Guid medicoId, string evolucion,
        IEnumerable<Diagnostico> diagnosticos, string tratamiento)
    {
        var atencion = Atencion.Crear(Id, medicoId, evolucion, diagnosticos, tratamiento);  // RN-01
        _atenciones.Add(atencion);
        return atencion;
    }
}

// Sigh.Hce.Dominio/Entidades/Diagnostico.cs
public sealed record Diagnostico(string CodigoCie10, string? Descripcion = null);
```

```csharp
// Sigh.Hce.Dominio/Contratos/IRepositorioHistorias.cs
using Sigh.Hce.Dominio.Entidades;

namespace Sigh.Hce.Dominio.Contratos;

public interface IRepositorioHistorias
{
    Task<HistoriaClinica?> ObtenerPorPacienteAsync(Guid pacienteId);
    Task GuardarAtencionAsync(Atencion atencion);
}

// Sigh.Hce.Dominio/Contratos/ICatalogoCie10.cs
public interface ICatalogoCie10
{
    Task<bool> ExisteCodigoAsync(string codigo);
}

// Sigh.Hce.Dominio/Contratos/IBitacoraAuditoria.cs
public interface IBitacoraAuditoria
{
    Task RegistrarAccesoAsync(Guid usuarioId, string accion, Guid historiaId);
}
```

### 6.2 Aplicación · caso de uso

```csharp
// Sigh.Hce.Aplicacion/RegistrarAtencion/RegistrarAtencionComando.cs
namespace Sigh.Hce.Aplicacion.RegistrarAtencion;

public sealed record RegistrarAtencionComando(
    Guid PacienteId,
    Guid MedicoId,
    string Evolucion,
    IReadOnlyList<string> CodigosCie10,
    string Tratamiento);
```

```csharp
// Sigh.Hce.Aplicacion/RegistrarAtencion/RegistrarAtencionCasoUso.cs
using Sigh.Compartido.Errores;
using Sigh.Hce.Dominio.Contratos;
using Sigh.Hce.Dominio.Entidades;

namespace Sigh.Hce.Aplicacion.RegistrarAtencion;

public sealed class RegistrarAtencionCasoUso
{
    private readonly IRepositorioHistorias _repositorio;         // interfaz, no PostgreSQL
    private readonly ICatalogoCie10 _cie10;                      // interfaz, no Redis
    private readonly IBitacoraAuditoria _bitacora;               // interfaz, no EF Core

    public RegistrarAtencionCasoUso(IRepositorioHistorias repositorio,
        ICatalogoCie10 cie10, IBitacoraAuditoria bitacora)
    {
        _repositorio = repositorio;
        _cie10 = cie10;
        _bitacora = bitacora;
    }

    public async Task<Guid> EjecutarAsync(RegistrarAtencionComando comando)
    {
        var historia = await _repositorio.ObtenerPorPacienteAsync(comando.PacienteId)
            ?? throw new RecursoNoEncontradoError("RN-01: el paciente no tiene historia clínica.");

        foreach (var codigo in comando.CodigosCie10)                                         // RN-02
        {
            if (!await _cie10.ExisteCodigoAsync(codigo))
                throw new ReglaNegocioError($"RN-02: el código CIE-10 '{codigo}' no es válido.");
        }

        var diagnosticos = comando.CodigosCie10.Select(codigo => new Diagnostico(codigo));
        var atencion = historia.RegistrarAtencion(comando.MedicoId, comando.Evolucion,
            diagnosticos, comando.Tratamiento);
        atencion.Firmar(comando.MedicoId);                                                   // RN-04

        await _repositorio.GuardarAtencionAsync(atencion);
        await _bitacora.RegistrarAccesoAsync(comando.MedicoId, "REGISTRAR_ATENCION", historia.Id);  // RN-06
        return atencion.Id;
    }
}
```

> En la implementación, el guardado de la atención y el registro en la bitácora deben ejecutarse en una misma transacción, para que no quede una atención sin su registro de auditoría. Se omite aquí para mantener el ejemplo corto.

### 6.3 Infraestructura · repositorio y decorador

```csharp
// Sigh.Hce.Infraestructura/Persistencia/RepositorioHistoriasEfCore.cs
using Microsoft.EntityFrameworkCore;
using Sigh.Hce.Dominio.Contratos;
using Sigh.Hce.Dominio.Entidades;

namespace Sigh.Hce.Infraestructura.Persistencia;

public sealed class RepositorioHistoriasEfCore : IRepositorioHistorias
{
    private readonly HceDbContext _contexto;

    public RepositorioHistoriasEfCore(HceDbContext contexto) => _contexto = contexto;

    public Task<HistoriaClinica?> ObtenerPorPacienteAsync(Guid pacienteId) =>
        _contexto.Historias.FirstOrDefaultAsync(h => h.PacienteId == pacienteId);

    public async Task GuardarAtencionAsync(Atencion atencion)
    {
        _contexto.Atenciones.Add(atencion);                      // EF Core genera el INSERT
        await _contexto.SaveChangesAsync();
    }
}
```

```csharp
// Sigh.Hce.Infraestructura/Catalogos/CatalogoCie10EnCache.cs
using StackExchange.Redis;
using Sigh.Hce.Dominio.Contratos;

namespace Sigh.Hce.Infraestructura.Catalogos;

public sealed class CatalogoCie10EnCache : ICatalogoCie10      // patrón Decorator
{
    private static readonly TimeSpan Duracion = TimeSpan.FromHours(12);
    private readonly ICatalogoCie10 _interno;                    // CatalogoCie10EfCore
    private readonly IDatabase _redis;

    public CatalogoCie10EnCache(ICatalogoCie10 interno, IConnectionMultiplexer redis)
    {
        _interno = interno;
        _redis = redis.GetDatabase();
    }

    public async Task<bool> ExisteCodigoAsync(string codigo)
    {
        var clave = $"cie10:{codigo.ToUpperInvariant()}";
        var enCache = await _redis.StringGetAsync(clave);
        if (enCache.HasValue)
            return enCache == "1";                               // encontrado en Redis

        var existe = await _interno.ExisteCodigoAsync(codigo);   // consulta en PostgreSQL
        await _redis.StringSetAsync(clave, existe ? "1" : "0", Duracion);
        return existe;
    }
}
```

### 6.4 Presentación · controlador

```csharp
// Sigh.Hce.Api/Solicitudes/RegistrarAtencionSolicitud.cs
public sealed record RegistrarAtencionSolicitud(
    string Evolucion, List<string> CodigosCie10, string Tratamiento);
```

```csharp
// Sigh.Hce.Api/Controladores/AtencionesController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigh.Hce.Aplicacion.RegistrarAtencion;

namespace Sigh.Hce.Api.Controladores;

[ApiController]
[Route("api/v1/pacientes/{pacienteId:guid}/atenciones")]
[Authorize(Roles = "Medico")]                                    // RN-03
public sealed class AtencionesController : ControllerBase
{
    private readonly RegistrarAtencionCasoUso _registrarAtencion;

    public AtencionesController(RegistrarAtencionCasoUso registrarAtencion) =>
        _registrarAtencion = registrarAtencion;

    [HttpPost]
    public async Task<IActionResult> Registrar(Guid pacienteId, RegistrarAtencionSolicitud solicitud)
    {
        var medicoId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);  // del token JWT, no del body

        var comando = new RegistrarAtencionComando(pacienteId, medicoId,
            solicitud.Evolucion, solicitud.CodigosCie10, solicitud.Tratamiento);

        var atencionId = await _registrarAtencion.EjecutarAsync(comando);
        return Created($"/api/v1/atenciones/{atencionId}", new { atencionId });
    }
}
```

> Los errores `RecursoNoEncontradoError` y `ReglaNegocioError` los traduce a `404` y `422` un manejador global de excepciones configurado en `Program.cs`, para no repetir esa lógica en cada controlador.

### 6.5 Raíz de composición

```csharp
// Sigh.Hce.Infraestructura/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Sigh.Hce.Dominio.Contratos;
using Sigh.Hce.Infraestructura.Auditoria;
using Sigh.Hce.Infraestructura.Catalogos;
using Sigh.Hce.Infraestructura.Persistencia;

namespace Sigh.Hce.Infraestructura;

public static class DependencyInjection
{
    public static IServiceCollection AgregarModuloHce(this IServiceCollection services,
        IConfiguration configuracion)
    {
        services.AddDbContext<HceDbContext>(opciones =>
            opciones.UseNpgsql(configuracion.GetConnectionString("SighOltp")));

        services.AddScoped<IRepositorioHistorias, RepositorioHistoriasEfCore>();  // en pruebas: RepositorioHistoriasEnMemoria
        services.AddScoped<IBitacoraAuditoria, BitacoraAuditoriaEfCore>();

        services.AddScoped<CatalogoCie10EfCore>();
        services.AddScoped<ICatalogoCie10>(sp => new CatalogoCie10EnCache(       // Decorator: Redis envuelve a EF Core
            sp.GetRequiredService<CatalogoCie10EfCore>(),
            sp.GetRequiredService<IConnectionMultiplexer>()));

        return services;
    }
}
```

```csharp
// Sigh.Api/Program.cs (fragmento)
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StackExchange.Redis;
using Sigh.Hce.Aplicacion.RegistrarAtencion;
using Sigh.Hce.Infraestructura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AgregarModuloHce(builder.Configuration);       // adaptadores del módulo HCE
builder.Services.AddScoped<RegistrarAtencionCasoUso>();         // casos de uso del módulo HCE
// builder.Services.AgregarModuloFarmacia(builder.Configuration);   ... resto de módulos

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones => { /* emisor, audiencia y clave desde appsettings.json */ });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```