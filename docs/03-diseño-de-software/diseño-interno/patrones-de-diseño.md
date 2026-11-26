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
| Adapter | Admisión y Triaje → Servicio APIPERU | APIPERU tiene su propio formato de petición y respuesta, y el proveedor podría cambiar | `HttpClient` de .NET con el token leído desde la configuración del entorno | `IServicioIdentidad.cs`, `AdaptadorApiPeru.cs` (producción), `AdaptadorIdentidadSimulado.cs` (pruebas) |
| Repository | Todos los módulos → Base transaccional | Las reglas clínicas no deben conocer SQL ni la base de datos | PostgreSQL con Entity Framework Core + Npgsql | `IRepositorioHistorias.cs`, `RepositorioHistoriasEfCore.cs` |
| Repository (solo lectura) | Reportes y Alertas → Datamart | Los reportes deben consultar el datamart sin acceder a la base transaccional | PostgreSQL (`sigh_olap`) con consultas de solo lectura | `IConsultasDatamart.cs`, `ConsultasDatamartPostgres.cs` |
| Decorator | Catálogos (CIE-10, medicamentos, exámenes) → Caché | Consultar los catálogos más rápido sin modificar el repositorio original | Redis (StackExchange.Redis) | `ICatalogoCie10.cs`, `CatalogoCie10EfCore.cs`, `CatalogoCie10EnCache.cs` |
| Facade | Admisión y Triaje, Farmacia y Laboratorio → HCE | Los módulos necesitan datos de la historia clínica sin conocer su estructura interna ni sus tablas | Interfaz pública del módulo HCE registrada con la inyección de dependencias de .NET | `IHceFachada.cs`, `HceFachada.cs` |
| Circuit Breaker | Admisión y Triaje → Servicio APIPERU | Si APIPERU falla o agotó su límite, cada admisión esperaría y reintentaría sin éxito | Política de resiliencia del `HttpClient` (Microsoft.Extensions.Http.Resilience): tiempo de espera, reintentos limitados y Circuit Breaker | `Program.cs`, `AdaptadorApiPeru.cs` |
| Observer (eventos de dominio) | Admisión, HCE, Farmacia, Laboratorio y alerta epidemiológica → Notificaciones | Los módulos deben avisar eventos sin conocer quién ni cómo se notifica | Eventos de dominio publicados en memoria y manejadores en el módulo de Notificaciones; envío en segundo plano con Web Push | `ResultadoLaboratorioRegistrado.cs`, `IManejadorEvento.cs`, `NotificarResultadoManejador.cs`, `EnviadorWebPush.cs` |
| CORS | Aplicación web → API SIGH | El navegador bloquea las peticiones a un origen distinto si la API no lo autoriza | Política CORS de ASP.NET Core que solo permite el origen del frontend del SIGH | `Program.cs` |

## Cómo funciona cada patrón

| Patrón | En pocas palabras |
|---|---|
| Adapter | Traduce: el sistema dice `ValidarDniAsync(dni)` y el adaptador lo convierte en la petición propia de APIPERU (con su token); en las pruebas, el adaptador simulado devuelve datos ficticios sin consumir el plan. |
| Repository | El negocio dice `GuardarAtencionAsync(atencion)`; el repositorio genera el SQL mediante EF Core. |
| Decorator | Busca primero el código CIE-10 en Redis; si no está, lo consulta en PostgreSQL y lo guarda en Redis para la próxima vez. Ambas clases cumplen la misma interfaz. |
| Facade | Farmacia pide `ObtenerRecetasPendientesAsync()` a la fachada de la HCE; la fachada coordina internamente sus casos de uso y devuelve solo lo necesario. |
| Circuit Breaker | Como un interruptor: si APIPERU falla varias veces seguidas, se deja de llamarlo por un tiempo y la admisión pasa directo al registro manual (RF02); luego se vuelve a probar. |
| Observer | Laboratorio solo dice "se registró un resultado"; el módulo de Notificaciones escucha ese evento, guarda el aviso en la bandeja y lo envía por Web Push al médico tratante, sin datos clínicos. |
| CORS | La API responde a la aplicación web del SIGH y rechaza peticiones de otros sitios desde el navegador. |

## Beneficio

- Cambiar el proveedor de identidad (APIPERU) o usar el simulado en las pruebas solo afecta al adaptador y a la configuración.
- Si APIPERU falla, el Circuit Breaker evita esperas y la admisión continúa con registro manual.
- Cambiar el canal de aviso (por ejemplo, de Web Push a correo) solo afecta al módulo de Notificaciones.
- Cambiar la base de datos o el ORM solo afecta a los repositorios.
- La caché con Redis se puede activar o retirar sin tocar las reglas clínicas.
- La HCE puede cambiar su estructura interna sin afectar a los módulos que dependen de ella, porque estos solo usan su fachada.
- Los reportes nunca acceden a la base transaccional, lo que mantiene el aislamiento OLTP / OLAP.

## Patrones previstos para siguientes iteraciones

| Patrón | Dónde se aplicaría | Cuándo |
|---|---|---|
| Strategy | Alerta epidemiológica: elegir el método de detección (umbral histórico, promedio móvil, etc.) | Si se necesita comparar o cambiar el algoritmo de detección |
| Strategy | Triaje: reglas de clasificación de prioridad I – IV | Si las reglas de clasificación cambian según el servicio (emergencia, consulta externa) |
| Factory | HCE: creación de historias clínicas y atenciones válidas | Si crear una atención se vuelve complejo (varios tipos de atención) |
