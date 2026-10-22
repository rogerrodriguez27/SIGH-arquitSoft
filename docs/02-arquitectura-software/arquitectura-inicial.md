# Arquitectura inicial del sistema

## Diagrama de arquitectura

```mermaid
flowchart TD

%% =========================
%% ACTORES
%% =========================
subgraph ACTORES["ACTORES"]
    Admision["Personal de Admisión"]
    Enfermeria["Enfermero(a) de Triaje"]
    Medico["Médico"]
    Farmaceutico["Farmacéutico"]
    Laboratorista["Personal de Laboratorio"]
    Epidemiologo["Epidemiólogo"]
    Directivo["Directivo"]
    Administrador["Administrador del sistema"]
end

%% =========================
%% PRESENTACIÓN
%% =========================
subgraph PRESENTACION["PRESENTACIÓN"]
    Web["Aplicación Web (React)"]
    API["API REST (C# / .NET)"]
end

%% =========================
%% LÓGICA DE NEGOCIO
%% =========================
subgraph NEGOCIO["LÓGICA DE NEGOCIO (monolito modular)"]
    AdmTriaje["Admisión y Triaje"]
    HCE["HCE (núcleo)"]
    Farmacia["Farmacia"]
    Laboratorio["Laboratorio"]
    Administracion["Administración"]
    Reportes["Reportes y Alertas"]
end

%% =========================
%% DATOS
%% =========================
subgraph DATOS["DATOS"]
    OLTP[("Base transaccional<br/>PostgreSQL - sigh_oltp")]
    Cache[("Caché<br/>Redis")]
    OLAP[("Datamart analítico<br/>PostgreSQL - sigh_olap")]
end

%% =========================
%% PROCESOS ANALÍTICOS
%% =========================
subgraph ANALITICA["PROCESOS ANALÍTICOS"]
    ETL["ETL<br/>(Python + cron)"]
    Alertas["Alerta epidemiológica<br/>(Python + cron)"]
    BI["Metabase (BI)"]
end

%% =========================
%% SISTEMAS EXTERNOS
%% =========================
subgraph EXTERNOS["SISTEMAS EXTERNOS"]
    RENIEC["Servicio RENIEC<br/>(simulado)"]
end

%% =========================
%% FLUJO PRINCIPAL
%% =========================
ACTORES --> Web
Web -->|HTTPS / JSON| API
API --> NEGOCIO
NEGOCIO -->|lectura y escritura| OLTP
NEGOCIO --> Cache

%% =========================
%% RELACIÓN CON EL NÚCLEO
%% =========================
AdmTriaje --> HCE
Farmacia --> HCE
Laboratorio --> HCE

%% =========================
%% FLUJO ANALÍTICO
%% =========================
OLTP -->|extrae| ETL
ETL -->|carga| OLAP
Alertas -->|lee y registra alertas| OLAP
Reportes -->|solo lectura| OLAP
BI -->|solo lectura| OLAP
Directivo -.-> BI
Epidemiologo -.-> BI

%% =========================
%% INTEGRACIONES EXTERNAS
%% =========================
AdmTriaje -->|valida DNI| RENIEC

%% =========================
%% ESTILOS
%% =========================
classDef nucleo fill:#fff3d6,stroke:#e0a100,stroke-width:3px,color:#111
class HCE nucleo
```

## Descripción

La arquitectura inicial se organiza en cuatro capas principales, más los procesos analíticos que se ejecutan de forma independiente:

- **Presentación:** permite la interacción de los usuarios con el sistema mediante la aplicación web en React y la API REST en C# / .NET.
- **Lógica de negocio:** un monolito modular que contiene los módulos Admisión y Triaje, HCE, Farmacia, Laboratorio, Administración, y Reportes y Alertas. La **HCE es el núcleo**: Admisión y Triaje, Farmacia y Laboratorio vinculan su información a la historia clínica del paciente.
- **Datos:** la base transaccional (OLTP) guarda la operación clínica diaria, la caché Redis acelera las consultas frecuentes y el datamart (OLAP) guarda la información agregada para el análisis. Las bases OLTP y OLAP están separadas físicamente.
- **Procesos analíticos:** el ETL carga periódicamente los datos de la base transaccional al datamart; el proceso de alerta epidemiológica analiza el datamart para detectar aumentos inusuales de diagnósticos, y Metabase muestra los dashboards. Ninguno de ellos accede a la base transaccional durante el análisis, por lo que no afectan la operación clínica.

Además, el módulo de **Admisión y Triaje** se integra con el **servicio de RENIEC** (simulado en la prueba de concepto) para validar el DNI de los pacientes.

## Componentes por capa

| Capa | Componentes | Tecnología |
|---|---|---|
| Presentación | Aplicación web, API REST | React, ASP.NET Core |
| Lógica de negocio | Admisión y Triaje, HCE, Farmacia, Laboratorio, Administración, Reportes y Alertas | C# / .NET (Clean Architecture por módulo) |
| Datos | Base transaccional, caché, datamart | PostgreSQL (`sigh_oltp`), Redis, PostgreSQL (`sigh_olap`) |
| Procesos analíticos | ETL, alerta epidemiológica, BI | Python + cron, Metabase |
| Sistemas externos | Servicio de identidad | RENIEC (simulado) |