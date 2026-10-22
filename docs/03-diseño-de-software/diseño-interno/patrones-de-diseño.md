# Patrones de diseño

## Objetivo

Documentar los patrones de diseño aplicados en el backend del SIGH, indicando dónde se usan, qué problema resuelven y con qué tecnología se implementan, para que todos los módulos se especifiquen y se generen con SDD de forma uniforme.

## Contexto

- **Estilo:** monolito modular (C# / .NET – ASP.NET Core).
- **Enfoque interno:** Clean Architecture.
- **Base:** diagrama de componentes (C4 nivel 3) y principios SOLID del módulo HCE.

## Patrones aplicados

| Patrón | Dónde se usa | Problema que resuelve | Se implementa con | Archivos en el código |
|---|---|---|---|---|
| Adapter | Admisión y Triaje → Servicio RENIEC | RENIEC tiene su propia API y, sin convenio, debe simularse | Datos ficticios en la prueba de concepto; `HttpClient` de .NET para el servicio real | `IServicioIdentidad.cs`, `AdaptadorReniecSimulado.cs`, `AdaptadorReniecReal.cs` (futuro) |
| Repository | Todos los módulos → Base transaccional | Las reglas clínicas no deben conocer SQL ni la base de datos | PostgreSQL con Entity Framework Core + Npgsql | `IRepositorioHistorias.cs`, `RepositorioHistoriasEfCore.cs` |
| Repository (solo lectura) | Reportes y Alertas → Datamart | Los reportes deben consultar el datamart sin acceder a la base transaccional | PostgreSQL (`sigh_olap`) con consultas de solo lectura | `IConsultasDatamart.cs`, `ConsultasDatamartPostgres.cs` |
| Decorator | Catálogos (CIE-10, medicamentos, exámenes) → Caché | Consultar los catálogos más rápido sin modificar el repositorio original | Redis (StackExchange.Redis) | `ICatalogoCie10.cs`, `CatalogoCie10EfCore.cs`, `CatalogoCie10EnCache.cs` |
| Facade | Admisión y Triaje, Farmacia y Laboratorio → HCE | Los módulos necesitan datos de la historia clínica sin conocer su estructura interna ni sus tablas | Interfaz pública del módulo HCE registrada con la inyección de dependencias de .NET | `IHceFachada.cs`, `HceFachada.cs` |

## Cómo funciona cada patrón

| Patrón | En pocas palabras |
|---|---|
| Adapter | Traduce: el sistema dice `ValidarDniAsync(dni)` y el adaptador lo convierte en la llamada propia de RENIEC, o devuelve datos ficticios si es la versión simulada. |
| Repository | El negocio dice `GuardarAtencionAsync(atencion)`; el repositorio genera el SQL mediante EF Core. |
| Decorator | Busca primero el código CIE-10 en Redis; si no está, lo consulta en PostgreSQL y lo guarda en Redis para la próxima vez. Ambas clases cumplen la misma interfaz. |
| Facade | Farmacia pide `ObtenerRecetasPendientesAsync()` a la fachada de la HCE; la fachada coordina internamente sus casos de uso y devuelve solo lo necesario. |

## Beneficio

- Reemplazar el servicio RENIEC simulado por el real solo afecta al adaptador.
- Cambiar la base de datos o el ORM solo afecta a los repositorios.
- La caché con Redis se puede activar o retirar sin tocar las reglas clínicas.
- La HCE puede cambiar su estructura interna sin afectar a los módulos que dependen de ella, porque estos solo usan su fachada.
- Los reportes nunca acceden a la base transaccional, lo que mantiene el aislamiento OLTP / OLAP.

## Patrones previstos para siguientes iteraciones

| Patrón | Dónde se aplicaría | Cuándo |
|---|---|---|
| Observer (eventos de dominio) | Laboratorio → HCE / médico: avisar cuando se registra un resultado | Al implementar las notificaciones dentro del sistema |
| Observer | Alerta epidemiológica → epidemiólogo: avisar cuando se genera una nueva alerta | Al implementar las notificaciones de alertas |
| Strategy | Alerta epidemiológica: elegir el método de detección (umbral histórico, promedio móvil, etc.) | Si se necesita comparar o cambiar el algoritmo de detección |
| Strategy | Triaje: reglas de clasificación de prioridad I – IV | Si las reglas de clasificación cambian según el servicio (emergencia, consulta externa) |
| Factory | HCE: creación de historias clínicas y atenciones válidas | Si crear una atención se vuelve complejo (varios tipos de atención) |