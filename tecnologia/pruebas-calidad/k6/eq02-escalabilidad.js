// EQ-02 (escalabilidad, AC02): la carga aumenta de forma progresiva hasta 200 usuarios simultáneos.
// Medida: el 95 % de las respuestas por debajo de 3 s y menos del 1 % de errores técnicos.
// Se ejecuta con una instancia de la API. La comparación con dos instancias queda pendiente: docs/ aún
// no define el balanceador de carga que las repartiría (principio XII).
import { sleep } from 'k6';
import { crearUsuarioListo, operacionesFrecuentes, prefijoEjecucion, tokenAdministrador } from './lib/comun.js';

const USUARIOS = 200;

export const options = {
  setupTimeout: '15m',
  scenarios: {
    carga_progresiva: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50 },
        { duration: '30s', target: 100 },
        { duration: '30s', target: 150 },
        { duration: '30s', target: 200 },
        { duration: '1m', target: 200 },
        { duration: '20s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    'http_req_duration{fase:medicion}': ['p(95)<3000'],
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
    usuarios.push(crearUsuarioListo(admin, `${prefijo}e${i}`, ['Administrador']));
  return { usuarios };
}

export default function (datos) {
  const usuario = datos.usuarios[(__VU - 1) % datos.usuarios.length];
  operacionesFrecuentes(usuario.token);
  sleep(1);
}
