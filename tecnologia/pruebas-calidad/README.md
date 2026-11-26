# Pruebas de atributos de calidad

Pruebas de los escenarios de [08-escenarios-atributos-calidad.md](../../docs/01-analisis-de-sistema/08-escenarios-atributos-calidad.md)
contra el sistema desplegado. Resultados medidos: [01-evidencias-atributos-calidad.md](../../docs/05-validacion-y-evidencias/01-evidencias-atributos-calidad.md).

| Archivo | Escenario | Qué mide |
|---|---|---|
| `k6/eq01-rendimiento.js` | EQ-01, SC-007 | 100 inicios de sesión simultáneos y 100 usuarios trabajando 2 minutos: p95 ≤ 2 s y < 1 % de errores |
| `k6/eq02-escalabilidad.js` | EQ-02 | Carga progresiva hasta 200 usuarios: p95 < 3 s y < 1 % de errores |
| `k6/eq04-seguridad.js` | EQ-04, SC-001 | 100 % de los accesos no autorizados (sin sesión, token manipulado, rol sin permiso) rechazados sin exponer información |
| `eq03-reinicio-api.ps1` | EQ-03 | La API se recupera sola en 60 s o menos tras una caída |
| `ejecutar-k6.ps1` | — | Ejecuta una prueba k6 con Docker y guarda los resultados sin credenciales |

## Requisitos

- Docker Desktop encendido (k6 se ejecuta con la imagen `grafana/k6`; no hace falta instalarlo).
- El sistema levantado:

  ```powershell
  cd tecnologia; docker compose --env-file ../.env up -d --build
  ```

- Un administrador **sin cambio de contraseña pendiente**. Inicia sesión una vez en la web y cambia la contraseña inicial.

## Ejecución

```powershell
cd tecnologia/pruebas-calidad
$env:ADMIN_USUARIO = 'admin'
./ejecutar-k6.ps1 -Prueba eq04-seguridad
```

El script pide la contraseña si no está en `ADMIN_CONTRASENA`. Repite con `-Prueba eq01-rendimiento` y
`-Prueba eq02-escalabilidad`.

```powershell
./eq03-reinicio-api.ps1 -Proyecto tecnologia
```

`-Proyecto` es el nombre del proyecto de Docker Compose: por defecto, el nombre de la carpeta.

Las pruebas crean usuarios ficticios con el prefijo `k6`. Úsalas en un entorno de prueba, no con datos
reales.
