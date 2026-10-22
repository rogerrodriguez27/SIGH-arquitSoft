# Nivel 4 · Diagrama de código

> **Proyecto:** SIGH · **Módulo detallado:** HCE (núcleo)  
> **Modelo:** C4 · Nivel 4 de 4 · **Notación:** UML · **Versión:** 1.0

---

## 1. Objetivo

Mostrar **cómo está organizado el código** de un componente del nivel 3: sus clases, interfaces y la forma en que colaboran. Se detalla el flujo **Registrar atención** del módulo **HCE**, que integra la base transaccional, el catálogo CIE-10 con caché en Redis y la bitácora de auditoría.

| Aspecto | Descripción |
|---|---|
| Pregunta que responde | ¿Qué clases existen dentro de un módulo y cómo se relacionan? |
| Audiencia | Desarrollo |
| Notación | UML (diagrama de clases y diagrama de secuencia). El modelo C4 recomienda UML para este nivel |
| Por qué solo el módulo HCE | El nivel 4 es opcional en C4 y se dibuja solo para los componentes más importantes. La HCE es el módulo núcleo obligatorio y concentra las reglas clínicas (RN-01 a RN-06); los demás módulos siguen la misma estructura |
| Nivel anterior | `Nivel3-Diagrama-de-Componentes.md` |

---

## 2. Diagrama de clases

![Diagrama de clases del módulo HCE](../../img/clases-modulo-hce.png)

> Archivo editable: `img/fuentes-svg/clases-modulo-hce.svg`

### 2.1 Capas de Clean Architecture

| Capa | Contiene | Regla |
|---|---|---|
| **Presentación** | Controladores y solicitudes | Traduce HTTP a llamadas del caso de uso y viceversa |
| **Aplicación** | Casos de uso, comandos y fachada del módulo | Coordina los pasos del proceso; no conoce la tecnología |
| **Dominio** | Entidades, objetos de valor, enumeraciones e interfaces | Contiene las reglas del negocio clínico; no referencia ninguna otra capa |
| **Infraestructura** | Repositorios, adaptadores y decoradores | Implementa las interfaces del dominio con una tecnología concreta |

> **Regla de dependencia:** las flechas del código apuntan hacia el dominio. Las clases de infraestructura implementan interfaces del dominio (triángulo hueco); el dominio nunca conoce a la infraestructura. En .NET esta regla se refuerza con las referencias entre proyectos.

### 2.2 Clases del módulo HCE

| Clase | Tipo | Capa | Responsabilidad |
|---|---|---|---|
| `AtencionesController` | Controlador | Presentación | Recibe `POST /api/v1/pacientes/{id}/atenciones` y responde al cliente |
| `RegistrarAtencionSolicitud` | DTO | Presentación | Cuerpo JSON de la petición |
| `RegistrarAtencionCasoUso` | Caso de uso | Aplicación | Valida la historia y los diagnósticos, registra y firma la atención, la guarda y la registra en la bitácora |
| `RegistrarAtencionComando` | DTO | Aplicación | Datos de entrada del caso de uso, incluido el `medicoId` obtenido del token |
| `HistoriaClinica` | Entidad (raíz) | Dominio | Agrupa las atenciones del paciente y es el único punto para crear una atención (RN-01) |
| `Atencion` | Entidad | Dominio | Registra evolución y diagnósticos, se firma y no se modifica después (RN-02, RN-04, RN-05) |
| `Diagnostico` | Objeto de valor | Dominio | Código CIE-10 y descripción |
| `NotaCorreccion` | Entidad | Dominio | Corrección vinculada a una atención firmada, con autor y fecha |
| `EstadoAtencion` | Enumeración | Dominio | BORRADOR, FIRMADA |
| `IRepositorioHistorias` | Interfaz | Dominio | Contrato para consultar historias y guardar atenciones |
| `ICatalogoCie10` | Interfaz | Dominio | Contrato para validar códigos CIE-10 |
| `IBitacoraAuditoria` | Interfaz | Dominio | Contrato para registrar accesos a la historia clínica (RN-06) |
| `RepositorioHistoriasEfCore` | Repositorio | Infraestructura | Implementa `IRepositorioHistorias` con EF Core y PostgreSQL |
| `CatalogoCie10EnCache` | Decorador | Infraestructura | Implementa `ICatalogoCie10` consultando primero Redis y luego `CatalogoCie10EfCore` |
| `CatalogoCie10EfCore` | Repositorio | Infraestructura | Implementa `ICatalogoCie10` consultando PostgreSQL |
| `BitacoraAuditoriaEfCore` | Repositorio | Infraestructura | Implementa `IBitacoraAuditoria` con EF Core y PostgreSQL |

### 2.3 Fachada pública del módulo HCE

| Clase | Tipo | Capa | Responsabilidad |
|---|---|---|---|
| `IHceFachada` | Interfaz (API pública) | Aplicación | Único punto por el que Admisión y Triaje, Farmacia y Laboratorio acceden a la HCE |
| `HceFachada` | Fachada | Aplicación | Coordina los casos de uso internos y devuelve solo los datos necesarios (recetas, órdenes, vínculo de resultados) |

### 2.4 Notación UML utilizada

| Símbolo | Significado | Ejemplo |
|---|---|---|
| Línea continua con flecha abierta | Asociación: la clase tiene una referencia a la otra | `RegistrarAtencionCasoUso` → `IRepositorioHistorias` |
| Línea discontinua con flecha abierta | Dependencia: la usa o la crea | `AtencionesController` «crea» `RegistrarAtencionComando` |
| Línea discontinua con triángulo hueco | Realización: implementa una interfaz | `RepositorioHistoriasEfCore` ▷ `IRepositorioHistorias` |
| Rombo relleno | Composición: la parte no existe sin el todo | `HistoriaClinica` ◆ `Atencion` |
| Nombre en cursiva con «interfaz» | Interfaz | `ICatalogoCie10` |
| «decorador» | Envuelve a otra implementación de la misma interfaz | `CatalogoCie10EnCache` «envuelve» `CatalogoCie10EfCore` |
| `-` y `+` | Atributo o método privado y público | `- Estado`, `+ Firmar(medicoId)` |

---

## 3. Diagrama de secuencia: registrar atención

![Diagrama de secuencia de registrar atención](../../img/secuencia-registrar-atencion.png)

> Archivo editable: `img/fuentes-svg/secuencia-registrar-atencion.svg`

| Paso | Origen → destino | Acción |
|---|---|---|
| 1 | Cliente web → `AtencionesController` | Envía `POST /api/v1/pacientes/{id}/atenciones` con la evolución, los códigos CIE-10, el tratamiento y el JWT (rol Médico, RN-03) |
| 2 | `AtencionesController` → `RegistrarAtencionCasoUso` | Ejecuta el caso de uso |
| 3 | `RegistrarAtencionCasoUso` → `IRepositorioHistorias` | Obtiene la historia clínica del paciente (RN-01) |
| 4 | `RegistrarAtencionCasoUso` → `ICatalogoCie10` | Valida cada código CIE-10, primero en Redis y luego en PostgreSQL (RN-02) |
| 5 | `RegistrarAtencionCasoUso` → `HistoriaClinica` | Registra la atención en estado BORRADOR |
| 6 | `RegistrarAtencionCasoUso` → `Atencion` | Firma la atención; desde ese momento no se modifica (RN-04) |
| 7–8 | `RegistrarAtencionCasoUso` → `IRepositorioHistorias` | Guarda la atención y sus diagnósticos en PostgreSQL |
| 9–10 | `RegistrarAtencionCasoUso` → `IBitacoraAuditoria` | Registra el acceso en la bitácora (RN-06) |
| 11 | `AtencionesController` → Cliente web | Responde `201 Created` con el identificador de la atención |
| 12–13 | Alternativa | Si la historia no existe o un código no es válido, la atención no se guarda y se responde `404 Not Found` o `422 Unprocessable Entity` |

---

## 4. Patrones y principios que se ven en este nivel

| Elemento del diagrama | Patrón o principio |
|---|---|
| `IRepositorioHistorias` y `RepositorioHistoriasEfCore` | Patrón **Repository** |
| `CatalogoCie10EnCache` envuelve a `CatalogoCie10EfCore` | Patrón **Decorator** |
| `IHceFachada` / `HceFachada` | Patrón **Facade** |
| El caso de uso recibe interfaces, no clases concretas | Principio de **inversión de dependencias** (SOLID) |
| Una clase por responsabilidad (controlador, caso de uso, entidad, repositorio) | Principio de **responsabilidad única** (SOLID) |
| Interfaces pequeñas: repositorio, catálogo y bitácora por separado | Principio de **segregación de interfaces** (SOLID) |
| `Atencion` con atributos privados y métodos que aplican las reglas | Encapsulamiento de las reglas clínicas (RN-01 a RN-06) |
| Otros módulos usan a la HCE solo mediante `IHceFachada` | Regla del **monolito modular**: comunicación por la API pública del módulo |

> Detalle completo en `03-diseño-de-software/diseño-interno/patrones-de-diseño.md`, `principios-de-diseño.md` y `diseño-interno-de-modulos.md`.

---