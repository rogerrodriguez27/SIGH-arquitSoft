import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { App } from '../App';
import { ProveedorSesion } from '../auth/ContextoSesion';
import { guardarSesion, type Sesion } from '../auth/sesion';

/** Sesión ficticia (principio IX) que vence en 30 minutos. */
export function sesionFicticia(cambios: Partial<Sesion['usuario']> = {}): Sesion {
  return {
    token: 'token-de-prueba',
    expiraEn: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
    usuario: {
      id: '00000000-0000-0000-0000-000000000001',
      nombreUsuario: 'med01',
      nombreCompleto: 'Médico Ficticio',
      roles: ['Medico'],
      permisos: ['hce.consultar', 'notificaciones.recibir'],
      cambioContrasenaPendiente: false,
      ...cambios,
    },
  };
}

export function conSesion(sesion: Sesion): void {
  guardarSesion(sesion);
}

/** Respuesta JSON simulada para fetch. */
export function respuestaJson(estado: number, cuerpo: unknown): Response {
  return new Response(JSON.stringify(cuerpo), {
    status: estado,
    headers: { 'Content-Type': estado >= 400 ? 'application/problem+json' : 'application/json' },
  });
}

export function renderizarApp(ruta: string) {
  return render(
    <MemoryRouter initialEntries={[ruta]}>
      <ProveedorSesion>
        <App />
      </ProveedorSesion>
    </MemoryRouter>,
  );
}
