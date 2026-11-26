# Especificación de funcionalidad: Administración — autenticación, usuarios, roles y bitácora de accesos

**Rama de la funcionalidad**: `001-admin-auth-rbac`

**Creada**: 2026-11-26

**Estado**: Borrador

**Entrada**: Descripción del usuario: "Módulo Administración del SIGH: inicio de sesión con JWT, gestión
de usuarios, roles y permisos (RBAC), y bitácora de accesos a la HCE. Implementa HU14 y HU16 y los
requisitos RF21, RF22 y RF24, respetando RN-06, ADR-007, DA02, EQ-04 y la constitución. Los catálogos
(HU15, RF23) quedan para una funcionalidad posterior. Basarse en docs/ como fuente de verdad."

**Trazabilidad**: HU14, HU16 · RF21, RF22, RF24 · RN-06 · DA02, DA07 · ADR-007, ADR-012 · AC04 ·
EQ-04, EQ-11 · RC15, RC16, RC18 · Constitución v1.0.1, principios II, III, VI, IX, X y XII.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Iniciar sesión en el sistema (Prioridad: P1)

Un miembro del personal del hospital (médico, enfermero(a) de triaje, personal de admisión,
farmacéutico, personal de laboratorio, epidemiólogo, directivo o administrador) ingresa su nombre de
usuario y contraseña. Si las credenciales son correctas y su cuenta está activa, el sistema le entrega
una sesión con sus roles y le muestra solo las funciones que su rol permite. Cuando termina, cierra
sesión (RF21).

**Por qué esta prioridad**: sin autenticación ningún otro módulo puede proteger la información
clínica (DA02). Es la base de todo el sistema.

**Prueba independiente**: con usuarios ficticios precargados, iniciar sesión con credenciales válidas e
inválidas y verificar que solo las válidas obtienen una sesión que da acceso a funciones protegidas.

**Escenarios de aceptación**:

1. **Dado** un usuario activo con credenciales válidas, **cuando** inicia sesión, **entonces** obtiene
   una sesión que identifica al usuario y sus roles, con una fecha de expiración.
2. **Dado** un usuario que ingresa una contraseña incorrecta, **cuando** intenta iniciar sesión,
   **entonces** el sistema rechaza el ingreso con un mensaje genérico que no revela si el usuario
   existe, y registra el intento fallido.
3. **Dado** un usuario desactivado, **cuando** intenta iniciar sesión con su contraseña correcta,
   **entonces** el sistema rechaza el ingreso.
4. **Dado** un usuario que falló 5 intentos seguidos, **cuando** vuelve a intentar dentro de los 15
   minutos siguientes, **entonces** el sistema rechaza el ingreso aunque la contraseña sea correcta.
5. **Dada** una sesión expirada, **cuando** el usuario intenta usar una función protegida,
   **entonces** el sistema la rechaza y le pide volver a iniciar sesión.
6. **Dado** un usuario sin sesión, **cuando** intenta acceder a cualquier función distinta del inicio
   de sesión, **entonces** el sistema rechaza la operación.

---

### Historia de usuario 2 - Gestionar usuarios y asignar roles (Prioridad: P1)

El administrador del sistema crea las cuentas del personal, les asigna uno o más roles, las
desactiva cuando la persona deja de trabajar en el hospital y restablece su contraseña cuando la
olvida, para que cada persona acceda solo a la información que le corresponde (HU14, RF22).

**Por qué esta prioridad**: sin usuarios con roles no se puede operar ni probar ningún otro módulo
del sistema con control de acceso.

**Prueba independiente**: el administrador crea un usuario con el rol Farmacéutico; ese usuario
inicia sesión y comprueba que puede usar funciones de farmacia y que se le niegan las de la HCE.

**Escenarios de aceptación**:

1. **Dado** un administrador con sesión activa, **cuando** registra un usuario con nombre completo,
   nombre de usuario único, contraseña inicial y al menos un rol, **entonces** el usuario queda activo
   y puede iniciar sesión.
2. **Dado** un nombre de usuario ya existente, **cuando** el administrador intenta registrar otro
   usuario con ese nombre, **entonces** el sistema lo rechaza.
3. **Dado** un usuario activo, **cuando** el administrador cambia sus roles, **entonces** sus nuevas
   sesiones reflejan los nuevos roles.
4. **Dado** un usuario activo, **cuando** el administrador lo desactiva, **entonces** ya no puede
   iniciar sesión y pierde el acceso a más tardar al expirar su sesión vigente.
5. **Dado** un usuario cualquiera, **cuando** el administrador intenta eliminarlo, **entonces** el
   sistema no ofrece la eliminación: solo la desactivación, para conservar la trazabilidad.
6. **Dado** un usuario que no tiene el rol Administrador, **cuando** intenta gestionar usuarios,
   **entonces** el sistema rechaza la operación y registra el intento.
7. **Dado** que solo queda un administrador activo, **cuando** se intenta desactivarlo o quitarle el
   rol Administrador, **entonces** el sistema lo impide.
8. **Dado** un usuario con contraseña restablecida por el administrador, **cuando** inicia sesión,
   **entonces** el sistema le exige cambiarla antes de usar otras funciones.

---

### Historia de usuario 3 - Aplicar permisos por rol en todo el sistema (Prioridad: P1)

Cada función del sistema está asociada a uno o más permisos, y cada rol tiene un conjunto de
permisos. Cuando un usuario intenta una operación, el sistema la permite solo si alguno de sus roles
tiene el permiso necesario (RF22, ADR-007, EQ-04).

**Por qué esta prioridad**: es el mecanismo que impide, por ejemplo, que un farmacéutico lea la
evolución médica de un paciente (EQ-04) y lo que exigen la Ley N° 29733 y la Ley N° 30024.

**Prueba independiente**: para cada rol, ejecutar una función permitida y otra no permitida, y
verificar que el 100 % de las no permitidas se rechaza.

**Escenarios de aceptación**:

1. **Dado** un usuario con el rol Médico, **cuando** consulta una función reservada al rol
   Médico, **entonces** el sistema la permite.
2. **Dado** un usuario con el rol Farmacéutico, **cuando** intenta consultar la evolución médica de
   una historia clínica, **entonces** el sistema la rechaza sin exponer información clínica y
   registra el intento en la bitácora.
3. **Dado** un usuario con dos roles, **cuando** usa una función permitida por cualquiera de ellos,
   **entonces** el sistema la permite.
4. **Dado** un administrador, **cuando** consulta la matriz de roles y permisos, **entonces** ve qué
   permisos tiene cada rol.
5. **Dado** un administrador, **cuando** consulta la matriz de roles y permisos, **entonces** no
   encuentra opciones para crear roles ni para editar permisos: los roles y la matriz son fijos y
   solo cambian con una nueva versión del sistema.

---

### Historia de usuario 4 - Registrar los accesos a las historias clínicas (Prioridad: P1)

Cada vez que cualquier usuario consulta o modifica una historia clínica, o intenta hacerlo sin
permiso, el sistema guarda quién lo hizo, cuándo, qué acción realizó, sobre qué historia y con qué
resultado (RN-06, RF24). Los demás módulos (HCE, Laboratorio, Farmacia, Admisión y Triaje) registran
sus accesos a través del servicio de bitácora que ofrece Administración.

**Por qué esta prioridad**: sin el registro no se cumple la trazabilidad exigida por la normativa
(RC16) y no se puede medir EQ-04. Debe existir antes de construir la HCE.

**Prueba independiente**: simular desde un módulo de prueba un acceso permitido y uno rechazado a
una historia ficticia, y verificar que ambos quedan registrados con todos sus datos.

**Escenarios de aceptación**:

1. **Dado** un módulo que registra un acceso a una historia clínica, **cuando** envía usuario,
   acción, historia y resultado, **entonces** la bitácora guarda la entrada con la fecha y hora del
   servidor.
2. **Dado** un intento de acceso rechazado por falta de permisos, **cuando** se procesa,
   **entonces** la bitácora guarda la entrada con resultado "denegado".
3. **Dada** una entrada existente en la bitácora, **cuando** cualquier usuario, incluido el
   administrador, intenta modificarla o eliminarla, **entonces** el sistema no lo permite.
4. **Dada** una operación clínica cuyo registro en la bitácora no puede guardarse, **cuando** se
   ejecuta, **entonces** la operación clínica tampoco se confirma (no hay acceso a la HCE sin
   registro).

---

### Historia de usuario 5 - Consultar la bitácora de accesos (Prioridad: P2)

El administrador consulta el registro de accesos a las historias clínicas filtrando por usuario,
historia clínica (o paciente), tipo de acción, resultado y rango de fechas, para garantizar la
trazabilidad y la confidencialidad (HU16, RF24).

**Por qué esta prioridad**: aporta valor de supervisión, pero depende de que la bitácora ya
registre los accesos (historia 4).

**Prueba independiente**: con entradas ficticias precargadas, filtrar por un usuario y un rango de
fechas y verificar que se obtienen exactamente las entradas esperadas, ordenadas de la más reciente
a la más antigua.

**Escenarios de aceptación**:

1. **Dado** un administrador, **cuando** consulta la bitácora con filtros, **entonces** obtiene las
   entradas que cumplen todos los filtros, paginadas y ordenadas de la más reciente a la más antigua.
2. **Dado** un usuario sin el rol Administrador, **cuando** intenta consultar la bitácora,
   **entonces** el sistema lo rechaza.
3. **Dado** que el administrador consulta la bitácora, **entonces** esa consulta también queda
   registrada como evento de seguridad.
4. **Dada** una entrada de la bitácora, **cuando** se muestra, **entonces** no incluye contenido
   clínico (diagnósticos, evoluciones, recetas, resultados): solo quién, cuándo, qué acción, sobre
   qué historia y con qué resultado.

---

### Casos límite

- Nombre de usuario con mayúsculas o espacios: se compara sin distinguir mayúsculas y sin espacios al
  inicio o al final.
- Primer arranque sin ningún usuario: el sistema crea un administrador inicial a partir de la
  configuración del entorno; si esa configuración falta, no se crea y se informa en el registro de
  arranque. Nunca hay credenciales por defecto en el código (constitución, principio VI).
- Sesión manipulada o con firma inválida: se rechaza como si no hubiera sesión.
- Usuario desactivado con una sesión vigente: pierde el acceso al vencer la sesión (máximo 30
  minutos); los cambios de rol también se aplican al iniciar una nueva sesión.
- Usuario cuyos roles fueron todos retirados: no puede quedar sin roles; el sistema exige al menos uno
  o la desactivación.
- Un usuario no puede desactivarse a sí mismo ni quitarse el rol Administrador.
- Consulta de bitácora con un rango de fechas invertido o mayor a un año: se rechaza con un mensaje
  claro.
- Muchos accesos simultáneos a la HCE: ningún acceso se pierde en la bitácora.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Autenticación (RF21)**

- **FR-001**: El sistema DEBE autenticar a los usuarios mediante nombre de usuario y contraseña, y
  entregar una sesión firmada que identifique al usuario y sus roles, sin guardar el estado de la
  sesión en el servidor (ADR-007, DA07).
- **FR-002**: La sesión DEBE expirar a los 30 minutos de emitida; el usuario debe volver a iniciar
  sesión para continuar.
- **FR-003**: El sistema DEBE rechazar con un mensaje genérico, sin indicar cuál dato es incorrecto,
  las credenciales inválidas, los usuarios inexistentes y los usuarios desactivados.
- **FR-004**: El sistema DEBE bloquear el inicio de sesión de un usuario durante 15 minutos tras 5
  intentos fallidos consecutivos.
- **FR-005**: El sistema DEBE guardar las contraseñas solo de forma irreversible (nunca en texto
  plano ni cifrado reversible) y exigir al menos 8 caracteres con letras y números.
- **FR-006**: El sistema DEBE permitir a cada usuario cambiar su propia contraseña indicando la
  actual, y exigir el cambio en el primer inicio de sesión tras una contraseña asignada por el
  administrador.
- **FR-007**: El sistema DEBE rechazar toda operación distinta del inicio de sesión que no venga con
  una sesión válida y vigente.

**Usuarios, roles y permisos (RF22, HU14)**

- **FR-008**: El administrador DEBE poder registrar, consultar, listar (con búsqueda por nombre o
  usuario y filtro por rol y estado), editar los datos básicos, desactivar y reactivar usuarios.
- **FR-009**: El sistema NO DEBE permitir eliminar usuarios; solo desactivarlos.
- **FR-010**: El administrador DEBE poder asignar a cada usuario uno o más roles; todo usuario activo
  tiene al menos un rol.
- **FR-011**: El sistema DEBE incluir los roles: Administrador, Personal de Admisión, Enfermero(a) de
  Triaje, Médico, Farmacéutico, Personal de Laboratorio, Epidemiólogo y Directivo (docs/01-actores).
- **FR-012**: El sistema DEBE asociar cada función protegida a un permiso y conceder la operación
  solo si alguno de los roles del usuario tiene ese permiso.
- **FR-013**: El sistema DEBE incluir una matriz de permisos por rol (ver "Matriz inicial de
  permisos") y permitir que el administrador la consulte. Los roles y la matriz son fijos: el
  administrador no puede crear, editar ni eliminar roles ni cambiar sus permisos; solo asigna roles a
  los usuarios. Cambiar la matriz requiere una nueva versión del sistema.
- **FR-014**: El sistema DEBE impedir que quede sin ningún administrador activo, y que un
  administrador se desactive a sí mismo o se quite su rol Administrador.
- **FR-015**: El administrador DEBE poder restablecer la contraseña de un usuario asignando una
  contraseña temporal.
- **FR-016**: El sistema DEBE crear, solo en el primer arranque y a partir de la configuración del
  entorno, un administrador inicial; no deben existir credenciales en el código ni en el repositorio.
- **FR-017**: Toda acción del administrador sobre usuarios, roles y permisos DEBE registrarse como
  evento de seguridad con quién, cuándo, sobre qué usuario o rol y qué cambió.

**Bitácora de accesos a la HCE (RF24, RN-06)**

- **FR-018**: El módulo Administración DEBE ofrecer a los demás módulos un servicio público para
  registrar accesos a historias clínicas; los demás módulos NO acceden directamente a los datos de la
  bitácora (constitución, principio II).
- **FR-019**: Cada entrada de la bitácora DEBE guardar: usuario, rol con el que actuó, fecha y hora
  del servidor, acción (por ejemplo, consultar historia, registrar atención, agregar nota de
  corrección, registrar resultado), historia clínica afectada, módulo de origen y resultado
  (permitido o denegado).
- **FR-020**: El sistema DEBE registrar también en la bitácora los intentos de acceso a historias
  clínicas rechazados por falta de permisos (EQ-04).
- **FR-021**: Las entradas de la bitácora NO DEBEN poder modificarse ni eliminarse por ninguna
  función del sistema.
- **FR-022**: El registro en la bitácora DEBE formar parte de la misma operación que el acceso a la
  historia clínica: si no se puede registrar, la operación no se confirma.
  > **Nota para el plan:** FR-022 exige que la operación clínica y su registro en la bitácora se
  > confirmen en la misma transacción, aunque pertenezcan a módulos distintos (por ejemplo, la
  > atención a la HCE y la bitácora a Administración). El plan DEBE definir cómo lograrlo sin que un
  > módulo acceda a las tablas del otro (constitución, principio II) y justificar la alternativa
  > elegida.
- **FR-023**: Las entradas de la bitácora NO DEBEN contener datos clínicos (diagnósticos,
  evoluciones, recetas, resultados) ni contraseñas o sesiones.
- **FR-024**: El administrador DEBE poder consultar la bitácora filtrando por usuario, historia
  clínica, acción, resultado y rango de fechas (máximo un año), con resultados paginados y ordenados
  del más reciente al más antiguo (HU16).
- **FR-025**: Solo el rol Administrador DEBE poder consultar la bitácora, y cada consulta DEBE
  registrarse como evento de seguridad.

**Eventos de seguridad**

- **FR-026**: El sistema DEBE registrar como eventos de seguridad, sin contraseñas: inicios de sesión
  exitosos y fallidos, bloqueos de cuenta, cambios de contraseña y las acciones de FR-017 y FR-025.

### Matriz inicial de permisos

Resuelve el estado "Pendiente de definir la matriz de permisos por rol" de EQ-04. Es fija (FR-013).
Las funciones de otros módulos se protegerán con estos permisos cuando se especifiquen.

| Permiso | Admin. | Admisión | Triaje | Médico | Farmac. | Laborat. | Epidem. | Directivo |
|---|---|---|---|---|---|---|---|---|
| Gestionar usuarios, roles y permisos | ✔ | | | | | | | |
| Consultar bitácora de accesos | ✔ | | | | | | | |
| Registrar y buscar pacientes, registrar ingreso | | ✔ | ✔ | | | | | |
| Registrar triaje | | | ✔ | | | | | |
| Consultar historia clínica completa | | | | ✔ | | | | |
| Registrar y firmar atención, nota de corrección | | | | ✔ | | | | |
| Emitir recetas y solicitar exámenes | | | | ✔ | | | | |
| Consultar recetas pendientes y dispensar | | | | | ✔ | | | |
| Gestionar inventario de medicamentos | | | | | ✔ | | | |
| Consultar órdenes y registrar resultados de laboratorio | | | | | | ✔ | | |
| Consultar reportes y revisar alertas epidemiológicas | | | | | | | ✔ | |
| Consultar dashboards de gestión | | | | | | | ✔ | ✔ |
| Recibir notificaciones y gestionar suscripción | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |

El Administrador no tiene acceso al contenido clínico de las historias (principio de mínimo
privilegio, RC15).

### Entidades clave

- **Usuario**: persona del hospital con acceso al sistema. Nombre completo, nombre de usuario único,
  credencial protegida, estado (activo / desactivado), indicador de cambio de contraseña pendiente,
  contador de intentos fallidos y fin del bloqueo, fechas de creación y último acceso. Tiene uno o
  más roles.
- **Rol**: agrupación fija de permisos que corresponde a un actor del hospital (por ejemplo, Médico).
  Nombre único y descripción; viene predefinido con el sistema.
- **Permiso**: autorización para ejecutar una función del sistema (por ejemplo, "Consultar historia
  clínica completa"). Código único y descripción. Se asigna a uno o más roles.
- **Entrada de bitácora de accesos a la HCE**: registro inmutable de un acceso o intento de acceso a
  una historia clínica (usuario, rol, fecha y hora, acción, historia, módulo de origen, resultado).
- **Evento de seguridad**: registro inmutable de inicios de sesión, bloqueos, cambios de contraseña
  y acciones administrativas (quién, cuándo, tipo de evento, objetivo y detalle sin secretos).

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **SC-001**: El 100 % de los intentos no autorizados incluidos en las pruebas (sin sesión, sesión
  inválida o vencida, rol sin permiso) es rechazado, sin exponer información clínica (EQ-04).
- **SC-002**: El 100 % de los accesos a historias clínicas ejecutados en las pruebas, permitidos y
  denegados, aparece en la bitácora con todos sus datos (EQ-04, RN-06).
- **SC-003**: El 100 % de los intentos de modificar o eliminar entradas de la bitácora es rechazado.
- **SC-004**: Un usuario del personal inicia sesión y llega a su pantalla principal en menos de 10
  segundos.
- **SC-005**: El administrador registra un usuario nuevo con sus roles en menos de 2 minutos.
- **SC-006**: El administrador encuentra los accesos de un usuario a una historia en un rango de
  fechas en menos de 1 minuto; el 95 % de las consultas a la bitácora responde en 2 segundos o menos
  con al menos 100 000 entradas ficticias.
- **SC-007**: El inicio de sesión mantiene el 95 % de las respuestas en 2 segundos o menos con 100
  usuarios simultáneos (EQ-01).
- **SC-008**: Ninguna contraseña, clave de firma ni credencial del administrador inicial aparece en el
  repositorio, en los registros ni en la bitácora (EQ-11).

## Aclaraciones

### Sesión 2026-11-26

- P: ¿Los roles y su matriz de permisos son fijos, o el administrador puede crear roles y editar sus
  permisos? → R: Fijos (opción A). El administrador solo asigna roles a los usuarios y consulta la
  matriz; los cambios de la matriz se hacen con una nueva versión del sistema.
- P: ¿Qué módulo es dueño de la bitácora de accesos a la HCE? → R: Administración. La HCE mantiene
  su puerto `IBitacoraAuditoria`, cuya implementación delega en el servicio público de FR-018
  (docs/ actualizados según el principio XII).

## Supuestos

- Fuera de alcance: los catálogos CIE-10, medicamentos y exámenes (HU15, RF23), que serán una
  funcionalidad posterior; la integración con proveedores de identidad externos o inicio de sesión
  único; la recuperación de contraseña por correo (la restablece el administrador); la autenticación
  de dos factores; y la revocación inmediata de sesiones (la sesión corta de 30 minutos limita la
  exposición, de acuerdo con la API sin estado de DA07).
- La pantalla de la bitácora pertenece al módulo Administración; las funciones de la HCE y de los
  demás módulos que registran accesos se construirán en sus propias funcionalidades usando el servicio
  de FR-018. En esta funcionalidad se prueba con accesos simulados.
- Las decisiones de diseño de la HCE (por ejemplo, quién firma una atención, RN-03) se aplican en ese
  módulo; aquí solo se provee el rol y el permiso.
- La bitácora y los eventos de seguridad se conservan sin purga durante la prueba de concepto.
- Todos los usuarios, pacientes e historias usados en pruebas y demostraciones son ficticios
  (RC14, constitución, principio IX).
- La comunicación con el sistema se realiza siempre por canal cifrado (TLS, ADR-007).
- Los mensajes y la interfaz están en español.
