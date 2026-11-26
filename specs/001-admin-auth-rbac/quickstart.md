# Quickstart: validar la funcionalidad 001 (Administración)

## Requisitos previos (en tu PC)

- .NET SDK 10. Compruébalo con `dotnet --version`.
- Node.js 22 LTS o superior. Compruébalo con `node --version`.
- Docker Desktop, encendido.
- El archivo `.env` en la raíz del repositorio, copiado de `.env.example` y completado:
  - `Jwt__ClaveFirma` con al menos 32 caracteres aleatorios;
  - `POSTGRES_PASSWORD`;
  - `ConnectionStrings__SighOltp`;
  - las tres variables `AdminInicial__*`.

  Nunca hagas commit de `.env`.

## 1. Pruebas automáticas (sin Docker)

```bash
cd tecnologia/backend
dotnet test --filter "Category!=Integracion"
```

**Resultado esperado:** todas en verde. Incluyen:
- las reglas de dominio: bloqueo, política de contraseñas, último administrador;
- los casos de uso;
- la autorización permitida y denegada para cada fila de la matriz (SC-001).

## 2. Pruebas de integración (con Docker)

```bash
dotnet test --filter "Category=Integracion"
```

**Resultado esperado:** en verde. Verifican:
- las migraciones;
- que `UPDATE` y `DELETE` sobre la bitácora fallen (SC-003);
- la transacción compartida de FR-022.

## 3. Levantar el sistema completo

```bash
cd tecnologia
docker compose --env-file ../.env up --build
```

- API: `http://localhost:8080`. Su healthcheck responde en `GET /salud`.
- Web: `http://localhost:5173`.

## 4. Validación manual (historias 1 a 5)

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | Inicia sesión con el administrador inicial | La aplicación pide cambiar la contraseña antes de continuar (FR-006) |
| 2 | Cambia la contraseña | Aparece el menú con Usuarios, Matriz de permisos y Bitácora |
| 3 | Crea un usuario `farm01` (ficticio) con el rol Farmacéutico | Se crea con estado Activo |
| 4 | Intenta crear otro usuario `FARM01 ` | Se rechaza por duplicado: los nombres se normalizan |
| 5 | Inicia sesión 5 veces con una contraseña incorrecta para `farm01` | La sexta vez se rechaza, aunque la contraseña sea correcta, durante 15 minutos |
| 6 | Como `farm01`, llama a `GET /api/v1/usuarios` | Responde `403` |
| 7 | Como administrador, intenta desactivarte a ti mismo | Responde `422` |
| 8 | Abre la Bitácora con un rango de 2 años | Responde `422` |
| 9 | Abre la Bitácora con el rango de hoy | Se ve la lista paginada, y la consulta queda como evento `BITACORA_CONSULTADA` |
| 10 | Ejecuta en PostgreSQL `DELETE FROM administracion.bitacora_accesos_hce;` | La base de datos lo rechaza (trigger) |

## 5. Comprobar que no hay secretos

```bash
git status            # .env no debe aparecer
git grep -n "ClaveFirma=" -- ':!*.example'   # no debe devolver ningún valor real
```

## Contratos

- [API REST](./contracts/api-administracion.openapi.yaml)
- [Servicio de bitácora para otros módulos](./contracts/servicio-bitacora.md)
- [Modelo de datos](./data-model.md)
