// Utilidades compartidas por las pruebas k6 de atributos de calidad (T118).
// Todos los usuarios que se crean son ficticios (principio IX) y las credenciales llegan por variables
// de entorno: nunca se escriben en el código (principio VI).
import http from 'k6/http';
import { check, fail } from 'k6';

export const API = (__ENV.API || 'http://localhost:8080').replace(/\/$/, '');
const JSON_HEADERS = { 'Content-Type': 'application/json' };

/** Contraseña ficticia que cumple la política (FR-005): letras y números, 8 o más caracteres. */
export function contrasenaAleatoria() {
  return 'K6' + Math.random().toString(36).slice(2, 12) + '9';
}

/** Prefijo único por ejecución para no repetir nombres de usuario. */
export function prefijoEjecucion() {
  return 'k6' + Date.now().toString(36);
}

export function encabezados(token) {
  return { headers: { ...JSON_HEADERS, Authorization: `Bearer ${token}` } };
}

export function iniciarSesion(nombreUsuario, contrasena, etiquetas = {}) {
  return http.post(`${API}/api/v1/autenticacion/iniciar-sesion`, JSON.stringify({ nombreUsuario, contrasena }), {
    headers: JSON_HEADERS,
    tags: { operacion: 'iniciar-sesion', ...etiquetas },
  });
}

/** Token del administrador indicado en ADMIN_USUARIO y ADMIN_CONTRASENA (sin cambio pendiente). */
export function tokenAdministrador() {
  const usuario = __ENV.ADMIN_USUARIO;
  const contrasena = __ENV.ADMIN_CONTRASENA;
  if (!usuario || !contrasena) fail('Defina ADMIN_USUARIO y ADMIN_CONTRASENA (administrador sin cambio pendiente).');
  const respuesta = iniciarSesion(usuario, contrasena, { fase: 'preparacion' });
  if (respuesta.status !== 200) fail(`No se pudo iniciar sesión como administrador: ${respuesta.status}`);
  if (respuesta.json('usuario.cambioContrasenaPendiente')) fail('El administrador tiene un cambio de contraseña pendiente.');
  return respuesta.json('token');
}

/**
 * Crea un usuario ficticio, inicia su sesión y hace el cambio de contraseña obligatorio, para dejarlo
 * listo para la prueba. Devuelve sus credenciales y un token sin cambio pendiente.
 */
export function crearUsuarioListo(tokenAdmin, nombreUsuario, roles) {
  const inicial = contrasenaAleatoria();
  const alta = http.post(
    `${API}/api/v1/usuarios`,
    JSON.stringify({ nombreUsuario, nombreCompleto: `Usuario Ficticio ${nombreUsuario}`, contrasenaInicial: inicial, roles }),
    { ...encabezados(tokenAdmin), tags: { fase: 'preparacion' } },
  );
  if (alta.status !== 201) fail(`No se pudo crear ${nombreUsuario}: ${alta.status} ${alta.body}`);

  const primera = iniciarSesion(nombreUsuario, inicial, { fase: 'preparacion' });
  const contrasena = contrasenaAleatoria();
  const cambio = http.post(
    `${API}/api/v1/autenticacion/cambiar-contrasena`,
    JSON.stringify({ contrasenaActual: inicial, contrasenaNueva: contrasena }),
    { ...encabezados(primera.json('token')), tags: { fase: 'preparacion' } },
  );
  if (cambio.status !== 200) fail(`No se pudo preparar ${nombreUsuario}: ${cambio.status}`);
  return { id: alta.json('id'), nombreUsuario, contrasena, token: cambio.json('token') };
}

/** Rango de fechas de hoy (UTC) para consultar la bitácora. */
export function rangoDeHoy() {
  const hoy = new Date().toISOString().slice(0, 10);
  return `desde=${hoy}T00:00:00Z&hasta=${hoy}T23:59:59Z`;
}

/** Operaciones protegidas de uso frecuente del administrador, con su etiqueta para el reporte. */
export function operacionesFrecuentes(token) {
  const opciones = (operacion) => ({ ...encabezados(token), tags: { operacion } });
  const respuestas = http.batch([
    ['GET', `${API}/api/v1/usuarios?pagina=1&tamano=20`, null, opciones('listar-usuarios')],
    ['GET', `${API}/api/v1/roles/matriz-permisos`, null, opciones('matriz-permisos')],
    ['GET', `${API}/api/v1/bitacora-accesos?${rangoDeHoy()}&pagina=1&tamano=20`, null, opciones('consultar-bitacora')],
  ]);
  check(respuestas[0], { 'listar usuarios → 200': (r) => r.status === 200 });
  check(respuestas[1], { 'matriz de permisos → 200': (r) => r.status === 200 });
  check(respuestas[2], { 'consultar bitácora → 200': (r) => r.status === 200 });
}
