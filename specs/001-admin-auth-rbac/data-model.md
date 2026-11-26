# Modelo de datos (Fase 1): módulo Administración

- **Base de datos**: `sigh_oltp`.
- **Esquema**: `administracion`, propiedad exclusiva del módulo (principio II).
- **Nombres**: las tablas y columnas usan `snake_case`.
- **Identificadores**: `uuid`, generados en el dominio.

## 1. Entidades del dominio

### Usuario (raíz de agregado)

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | Guid | Se genera al crear el usuario |
| `NombreUsuario` | texto (3–50) | Único. Se normaliza: se quitan los espacios de los extremos y se pasa a minúsculas. Solo se permiten letras, números, `.`, `_` y `-` |
| `NombreCompleto` | texto (3–150) | Obligatorio |
| `HashContrasena` | texto | Lo calcula `IHasherContrasenas`. Nunca se expone fuera del módulo |
| `Estado` | `Activo` \| `Desactivado` | No existe el estado "eliminado" (FR-009) |
| `CambioContrasenaPendiente` | bool | `true` al crear el usuario o al restablecer su contraseña (FR-006, FR-015) |
| `IntentosFallidos` | int | Se reinicia con un inicio de sesión exitoso |
| `BloqueadoHasta` | DateTimeOffset? | Se fija en ahora + 15 min al quinto intento fallido consecutivo (FR-004) |
| `Roles` | conjunto de `CodigoRol` | Al menos uno (FR-010) |
| `CreadoEn` | DateTimeOffset | Se toma de `IReloj` |
| `UltimoAcceso` | DateTimeOffset? | Se actualiza con cada inicio de sesión exitoso |

**Operaciones y reglas:**

- `Crear(...)`: valida los datos, exige al menos un rol y deja la cuenta en estado `Activo` con el
  cambio de contraseña pendiente.
- `IntentarAutenticar(contrasena, hasher, reloj)`:
  - rechaza si la cuenta está `Desactivado`;
  - rechaza si `BloqueadoHasta` es mayor que ahora;
  - si el hash no coincide, suma un intento fallido y bloquea la cuenta al llegar a 5;
  - si coincide, reinicia el contador y actualiza `UltimoAcceso`.
  - Devuelve un resultado con el motivo, que es interno: la API siempre responde con un mensaje
    genérico.
- `CambiarContrasena(actual, nueva)`: verifica la contraseña actual, aplica la política de
  `ContrasenaPropuesta` y quita `CambioContrasenaPendiente`.
- `RestablecerContrasena(temporal)`: pone `CambioContrasenaPendiente` en `true` y reinicia el
  bloqueo.
- `AsignarRoles(roles)`: exige al menos un rol.
- `Desactivar()` / `Reactivar()`.

### Reglas que involucran a varios usuarios (servicio de dominio `PoliticaAdministradores`)

- No se puede desactivar ni quitar el rol `Administrador` al último administrador activo (FR-014).
- Un administrador no puede desactivarse a sí mismo ni quitarse su propio rol `Administrador`.

### CodigoRol (enumeración fija, FR-011)

`Administrador`, `PersonalAdmision`, `EnfermeroTriaje`, `Medico`, `Farmaceutico`,
`PersonalLaboratorio`, `Epidemiologo`, `Directivo`.

### CodigoPermiso (enumeración fija)

| Código | Descripción |
|---|---|
| `usuarios.gestionar` | Gestionar usuarios, roles y permisos |
| `bitacora.consultar` | Consultar la bitácora de accesos |
| `pacientes.registrar` | Registrar y buscar pacientes, registrar el ingreso |
| `triaje.registrar` | Registrar el triaje |
| `hce.consultar` | Consultar la historia clínica completa |
| `hce.registrar_atencion` | Registrar y firmar una atención o una nota de corrección |
| `hce.recetas_examenes` | Emitir recetas y solicitar exámenes |
| `farmacia.dispensar` | Consultar las recetas pendientes y dispensar |
| `farmacia.inventario` | Gestionar el inventario de medicamentos |
| `laboratorio.resultados` | Consultar las órdenes y registrar los resultados de laboratorio |
| `epidemiologia.reportes` | Consultar reportes y revisar alertas epidemiológicas |
| `bi.dashboards` | Consultar los dashboards de gestión |
| `notificaciones.recibir` | Recibir notificaciones y gestionar la suscripción |

### MatrizPermisos (valor estático del dominio)

- Es la traducción literal de la tabla "Matriz inicial de permisos" de la spec.
- Operaciones:
  - `PermisosDe(roles)`: devuelve la unión de los permisos de todos los roles del usuario
    (escenario 3 de la historia 3).
  - `Filas()`: entrega la matriz para mostrarla en la consulta.
- No se guarda en la base de datos: los roles y los permisos son fijos (aclaración Q1 = A). Se
  copian a tablas de catálogo solo para que los informes SQL puedan leerlos (ver la sección 2).

### EntradaBitacora (inmutable, RF24 y RN-06)

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | Guid | |
| `UsuarioId` | Guid | Usuario que hizo el acceso |
| `NombreUsuario` | texto | Copia del nombre al momento del acceso, para leer la bitácora sin hacer *joins* |
| `Rol` | `CodigoRol` | Rol que otorgó el permiso usado. Si se denegó, el primer rol del usuario |
| `FechaHora` | DateTimeOffset | Hora del servidor (`IReloj`); nunca la envía el cliente |
| `Accion` | texto (código) | Lista cerrada: `CONSULTAR_HISTORIA`, `REGISTRAR_ATENCION`, `AGREGAR_NOTA_CORRECCION`, `REGISTRAR_RESULTADO`, `DISPENSAR_RECETA`, `REGISTRAR_INGRESO`, `REGISTRAR_TRIAJE`. Ampliable por versión |
| `HistoriaId` | Guid | Historia clínica afectada |
| `ModuloOrigen` | texto | `Hce`, `Admision`, `Farmacia`, `Laboratorio` |
| `Resultado` | `Permitido` \| `Denegado` | |

No guarda datos clínicos (FR-023). Una vez creada, no tiene operaciones de modificación (FR-021).

### EventoSeguridad (inmutable, FR-017, FR-025 y FR-026)

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | Guid | |
| `FechaHora` | DateTimeOffset | Hora del servidor |
| `Tipo` | código | `INICIO_SESION_EXITOSO`, `INICIO_SESION_FALLIDO`, `CUENTA_BLOQUEADA`, `CONTRASENA_CAMBIADA`, `CONTRASENA_RESTABLECIDA`, `USUARIO_CREADO`, `USUARIO_EDITADO`, `ROLES_CAMBIADOS`, `USUARIO_DESACTIVADO`, `USUARIO_REACTIVADO`, `BITACORA_CONSULTADA`, `ACCESO_DENEGADO` |
| `ActorId` | Guid? | Usuario que actúa. Es nulo si el inicio de sesión falla con un usuario inexistente |
| `ObjetivoId` | Guid? | Usuario afectado, si corresponde |
| `Detalle` | texto (≤ 500) | Descripción sin secretos ni datos clínicos. Por ejemplo: `roles: Medico → Medico, Epidemiologo` o `filtros: usuario=…, desde=…` |

## 2. Tablas (esquema `administracion`)

| Tabla | Columnas principales | Restricciones e índices |
|---|---|---|
| `usuarios` | `id`, `nombre_usuario`, `nombre_completo`, `hash_contrasena`, `estado`, `cambio_contrasena_pendiente`, `intentos_fallidos`, `bloqueado_hasta`, `creado_en`, `ultimo_acceso`, `version` | PK `id`. `UNIQUE (nombre_usuario)`. `CHECK (estado IN ('Activo','Desactivado'))`. `version` es un *token* de concurrencia optimista |
| `usuario_roles` | `usuario_id`, `rol` | PK `(usuario_id, rol)`. FK a `usuarios` con `ON DELETE RESTRICT`. `CHECK` de que el rol esté en la lista de 8 |
| `roles` | `codigo`, `nombre`, `descripcion` | Catálogo de solo lectura que crea la migración |
| `permisos` | `codigo`, `descripcion` | Catálogo de solo lectura que crea la migración |
| `rol_permisos` | `rol`, `permiso` | Catálogo de solo lectura que crea la migración, copia de `MatrizPermisos` |
| `bitacora_accesos_hce` | `id`, `usuario_id`, `nombre_usuario`, `rol`, `fecha_hora`, `accion`, `historia_id`, `modulo_origen`, `resultado` | Índices: `(fecha_hora DESC)`, `(usuario_id, fecha_hora DESC)`, `(historia_id, fecha_hora DESC)`. Trigger `bloquear_modificacion` en UPDATE y DELETE |
| `eventos_seguridad` | `id`, `fecha_hora`, `tipo`, `actor_id`, `objetivo_id`, `detalle` | Índice `(fecha_hora DESC)`. Trigger `bloquear_modificacion` en UPDATE y DELETE |

**Notas:**

- `historia_id` no tiene FK hacia el esquema `hce`. Las referencias entre módulos se guardan solo
  como identificadores (principio II).
- Las tablas `roles`, `permisos` y `rol_permisos` sirven para consultas SQL. La aplicación decide
  los permisos con `MatrizPermisos`. Una prueba verifica que ambas coinciden.

## 3. Transiciones de estado del usuario

```text
            Crear (cambio pendiente)
                    │
                    ▼
   ┌──────────► Activo ◄──────────────┐
   │             │  ▲                  │
   │  Desactivar │  │ Reactivar        │ 5 fallos → BloqueadoHasta = ahora + 15 min
   │             ▼  │                  │ (sigue en estado Activo; se desbloquea solo)
   │          Desactivado              │
   └───────────────────────────────────┘
```

- Un inicio de sesión solo es exitoso si se cumplen las tres condiciones:
  - `Estado = Activo`;
  - la cuenta no está bloqueada;
  - el hash de la contraseña coincide.
- Si `CambioContrasenaPendiente = true`, la sesión solo permite cambiar la contraseña y cerrar
  sesión.

## 4. Validaciones (resumen por requisito)

| Requisito | Dónde se valida |
|---|---|
| FR-003, FR-004 | `Usuario.IntentarAutenticar` y el caso de uso `IniciarSesion` (mensaje genérico) |
| FR-005 | `ContrasenaPropuesta` (dominio) e `IHasherContrasenas` (infraestructura) |
| FR-009 | No existe ningún caso de uso ni endpoint de eliminación. FK con `ON DELETE RESTRICT` |
| FR-010, FR-014 | `Usuario.AsignarRoles` y `PoliticaAdministradores` |
| FR-021 | Dominio sin métodos de modificación, repositorio de solo inserción y *trigger* en la base de datos |
| FR-022 | `IUnidadDeTrabajo` compartida (research D3) |
| FR-023 | `EntradaBitacora` sin campos clínicos. La prueba de contrato revisa los campos de la respuesta |
| FR-024 | `ConsultarBitacoraCasoUso`: rango obligatorio de un año o menos, desde ≤ hasta, tamaño de página ≤ 100 |
