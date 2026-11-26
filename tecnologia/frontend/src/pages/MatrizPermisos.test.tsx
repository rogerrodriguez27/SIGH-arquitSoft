import { screen, within } from '@testing-library/react';
import { conSesion, renderizarApp, respuestaJson, sesionFicticia } from '../pruebas/utilidades';

const matriz = {
  roles: [
    { codigo: 'Administrador', nombre: 'Administrador' },
    { codigo: 'Medico', nombre: 'Médico' },
  ],
  permisos: [
    { codigo: 'usuarios.gestionar', descripcion: 'Gestionar usuarios, roles y permisos', roles: ['Administrador'] },
    { codigo: 'hce.consultar', descripcion: 'Consultar la historia clínica completa', roles: ['Medico'] },
  ],
};

describe('Matriz de permisos', () => {
  beforeEach(() => {
    sessionStorage.clear();
    conSesion(sesionFicticia({ roles: ['Administrador'], permisos: ['usuarios.gestionar', 'notificaciones.recibir'] }));
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respuestaJson(200, matriz)));
  });
  afterEach(() => vi.unstubAllGlobals());

  it('muestra qué permisos tiene cada rol', async () => {
    renderizarApp('/matriz-permisos');

    const fila = (await screen.findByRole('rowheader', { name: 'Consultar la historia clínica completa' })).closest('tr')!;
    const celdas = within(fila).getAllByRole('cell');
    expect(celdas[0]).toHaveAccessibleName('No');
    expect(celdas[1]).toHaveAccessibleName('Sí');
    expect(screen.getByRole('link', { name: 'Matriz de permisos' })).toBeInTheDocument();
  });

  it('es de solo lectura: no ofrece crear ni editar roles o permisos', async () => {
    renderizarApp('/matriz-permisos');
    await screen.findByRole('table');

    const contenido = screen.getByRole('main');
    expect(within(contenido).queryAllByRole('button')).toHaveLength(0);
    expect(within(contenido).queryAllByRole('textbox')).toHaveLength(0);
    expect(within(contenido).queryAllByRole('checkbox')).toHaveLength(0);
  });
});
