// EQ-01 (rendimiento, AC01) y SC-007: 100 usuarios simultáneos.
// Medida: el 95 % de las respuestas en 2 s o menos y menos del 1 % de errores técnicos.
// En esta funcionalidad aún no existen las operaciones clínicas (HCE, Admisión): se miden el inicio
// de sesión y las operaciones protegidas del módulo Administración, que pasan por el mismo camino
// (JWT, autorización por permiso, EF Core y PostgreSQL).
import { check, sleep } from 'k6';
import exec from 'k6/execution';
import { crearUsuarioListo, iniciarSesion, operacionesFrecuentes, prefijoEjecucion, tokenAdministrador } from './lib/comun.js';

const USUARIOS = 100;

export const options = {
  setupTimeout: '10m',
  scenarios: {
    // SC-007: los 100 usuarios inician sesión a la vez.
    inicios_de_sesion_simultaneos: {
      executor: 'per-vu-iterations',
      exec: 'inicioDeSesion',
      vus: USUARIOS,
      iterations: 1,
      maxDuration: '1m',
    },
    // EQ-01: 100 usuarios con sesión trabajando durante 2 minutos.
    operaciones_con_100_usuarios: {
      executor: 'constant-vus',
      exec: 'operaciones',
      vus: USUARIOS,
      duration: '2m',
      startTime: '20s',
    },
  },
  thresholds: {
    'http_req_duration{scenario:inicios_de_sesion_simultaneos}': ['p(95)<2000'],
    'http_req_duration{scenario:operaciones_con_100_usuarios}': ['p(95)<2000'],
    'http_req_failed{fase:medicion}': ['rate<0.01'],
    checks: ['rate==1'],
  },
  tags: { fase: 'medicion' },
};

export function setup() {
  const admin = tokenAdministrador();
  const prefijo = prefijoEjecucion();
  const usuarios = [];
  for (let i = 0; i < USUARIOS; i++)
    usuarios.push(crearUsuarioListo(admin, `${prefijo}u${i}`, ['Administrador']));
  return { usuarios };
}

export function inicioDeSesion(datos) {
  // Un usuario distinto por iteración: __VU es global a todos los escenarios y podría repetirse.
  const usuario = datos.usuarios[exec.scenario.iterationInTest % datos.usuarios.length];
  const respuesta = iniciarSesion(usuario.nombreUsuario, usuario.contrasena);
  check(respuesta, { 'inicio de sesión → 200 con token': (r) => r.status === 200 && !!r.json('token') });
}

export function operaciones(datos) {
  const usuario = datos.usuarios[(__VU - 1) % datos.usuarios.length];
  operacionesFrecuentes(usuario.token);
  sleep(1);                                                          // tiempo de lectura del usuario
}
