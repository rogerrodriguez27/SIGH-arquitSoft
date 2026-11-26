// EQ-04 (seguridad, confidencialidad, AC04) y SC-001: el 100 % de los intentos no autorizados
// (sin sesión, sesión manipulada o rol sin permiso) es rechazado, sin exponer información, contra el
// sistema desplegado. Cada 403 queda además como evento ACCESO_DENEGADO (se revisa en la base de datos).
import http from 'k6/http';
import { check } from 'k6';
import { API, crearUsuarioListo, encabezados, prefijoEjecucion, rangoDeHoy, tokenAdministrador } from './lib/comun.js';

const ROLES = ['Administrador', 'PersonalAdmision', 'EnfermeroTriaje', 'Medico', 'Farmaceutico',
  'PersonalLaboratorio', 'Epidemiologo', 'Directivo'];

export const options = {
  setupTimeout: '5m',
  scenarios: { verificacion: { executor: 'shared-iterations', vus: 1, iterations: 1 } },
  thresholds: {
    checks: ['rate==1'],                                             // 100 % de los casos como se espera
    'checks{caso:no-autorizado}': ['rate==1'],
  },
};

/** Endpoints protegidos del módulo y el permiso que exigen (matriz fija, FR-013). */
function endpoints(idObjetivo) {
  return [
    { nombre: 'GET usuarios', metodo: 'GET', url: `${API}/api/v1/usuarios`, permiso: 'Administrador' },
    { nombre: 'POST usuarios', metodo: 'POST', url: `${API}/api/v1/usuarios`, permiso: 'Administrador',
      cuerpo: { nombreUsuario: 'nunca', nombreCompleto: 'No Creado', contrasenaInicial: 'NoCreado2026', roles: ['Medico'] } },
    { nombre: 'PUT roles', metodo: 'PUT', url: `${API}/api/v1/usuarios/${idObjetivo}/roles`, permiso: 'Administrador',
      cuerpo: { roles: ['Directivo'] } },
    { nombre: 'POST desactivar', metodo: 'POST', url: `${API}/api/v1/usuarios/${idObjetivo}/desactivar`, permiso: 'Administrador' },
    { nombre: 'GET matriz', metodo: 'GET', url: `${API}/api/v1/roles/matriz-permisos`, permiso: 'Administrador' },
    { nombre: 'GET bitácora', metodo: 'GET', url: `${API}/api/v1/bitacora-accesos?${rangoDeHoy()}`, permiso: 'Administrador' },
  ];
}

function enviar(e, parametros) {
  const cuerpo = e.cuerpo ? JSON.stringify(e.cuerpo) : null;
  return http.request(e.metodo, e.url, cuerpo, parametros);
}

/** Un rechazo no expone datos: solo problem+json con title/status, sin tokens ni contenido. */
function sinInformacion(r) {
  const tipo = r.headers['Content-Type'] || '';
  return tipo.includes('application/problem+json') && !r.body.includes('token') && !r.body.includes('elementos');
}

export function setup() {
  const admin = tokenAdministrador();
  const prefijo = prefijoEjecucion();
  const porRol = {};
  ROLES.forEach((rol, i) => { porRol[rol] = crearUsuarioListo(admin, `${prefijo}r${i}`, [rol]); });
  const objetivo = crearUsuarioListo(admin, `${prefijo}obj`, ['Directivo']);
  return { porRol, idObjetivo: objetivo.id };
}

export default function (datos) {
  const lista = endpoints(datos.idObjetivo);
  let total = 0;

  for (const e of lista) {
    // 1) Sin sesión → 401
    const sinToken = enviar(e, { headers: { 'Content-Type': 'application/json' }, tags: { caso: 'no-autorizado' } });
    check(sinToken, { [`${e.nombre} sin sesión → 401 sin información`]: (r) => r.status === 401 && sinInformacion(r) }, { caso: 'no-autorizado' });

    // 2) Token manipulado (firma alterada) → 401
    const token = datos.porRol.Medico.token;
    const manipulado = token.slice(0, -4) + (token.endsWith('AAAA') ? 'BBBB' : 'AAAA');
    const conManipulado = enviar(e, { ...encabezados(manipulado), tags: { caso: 'no-autorizado' } });
    check(conManipulado, { [`${e.nombre} con token manipulado → 401`]: (r) => r.status === 401 }, { caso: 'no-autorizado' });
    total += 2;

    // 3) Cada rol sin el permiso → 403 sin información
    for (const rol of ROLES.filter((r) => r !== e.permiso)) {
      const r = enviar(e, { ...encabezados(datos.porRol[rol].token), tags: { caso: 'no-autorizado' } });
      check(r, { [`${e.nombre} como ${rol} → 403 sin información`]: (x) => x.status === 403 && sinInformacion(x) }, { caso: 'no-autorizado' });
      total++;
    }
  }

  // 4) Control positivo: el rol con permiso sí accede a las consultas.
  for (const e of lista.filter((x) => x.metodo === 'GET')) {
    const r = enviar(e, { ...encabezados(datos.porRol.Administrador.token), tags: { caso: 'autorizado' } });
    check(r, { [`${e.nombre} como Administrador → 200`]: (x) => x.status === 200 }, { caso: 'autorizado' });
  }

  console.log(`Intentos no autorizados ejecutados: ${total}`);
}
