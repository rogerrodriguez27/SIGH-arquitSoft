import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { EntradaBitacora } from '../api/bitacora';
import { conSesion, renderizarApp, respuestaJson, sesionFicticia } from '../pruebas/utilidades';

const entrada: EntradaBitacora = {
  id: '00000000-0000-0000-0000-0000000000b1',
  usuarioId: '00000000-0000-0000-0000-0000000000aa',
  nombreUsuario: 'farm01',
  rol: 'Farmaceutico',
  fechaHora: '2026-11-26T08:00:00Z',
  accion: 'CONSULTAR_HISTORIA',
  historiaId: '00000000-0000-0000-0000-0000000000c1',
  moduloOrigen: 'Hce',
  resultado: 'Denegado',
};

describe('Bitácora', () => {
  beforeEach(() => {
    sessionStorage.clear();
    conSesion(
      sesionFicticia({
        roles: ['Administrador'],
        permisos: ['usuarios.gestionar', 'bitacora.consultar', 'notificaciones.recibir'],
      }),
    );
  });
  afterEach(() => vi.unstubAllGlobals());

  it('consulta con los filtros y muestra las entradas sin contenido clínico', async () => {
    const fetchSimulado = vi.fn().mockResolvedValue(
      respuestaJson(200, { elementos: [entrada], pagina: 1, tamano: 20, total: 1 }),
    );
    vi.stubGlobal('fetch', fetchSimulado);
    renderizarApp('/bitacora');

    await userEvent.selectOptions(screen.getByLabelText('Resultado'), 'Denegado');
    await userEvent.type(screen.getByLabelText('ID de usuario'), entrada.usuarioId);
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }));

    const fila = (await screen.findByText('farm01')).closest('tr')!;
    expect(within(fila).getByText('Farmacéutico')).toBeInTheDocument();
    expect(within(fila).getByText('Consultar historia')).toBeInTheDocument();
    expect(within(fila).getByText('Denegado')).toBeInTheDocument();
    expect(screen.getAllByRole('columnheader').map((c) => c.textContent)).toEqual([
      'Fecha y hora', 'Usuario', 'Rol', 'Acción', 'Historia clínica', 'Módulo', 'Resultado',
    ]);

    const url = new URL(fetchSimulado.mock.calls[0][0] as string);
    expect(url.pathname).toBe('/api/v1/bitacora-accesos');
    expect(url.searchParams.get('resultado')).toBe('Denegado');
    expect(url.searchParams.get('usuarioId')).toBe(entrada.usuarioId);
    expect(url.searchParams.get('desde')).not.toBeNull();
    expect(url.searchParams.get('hasta')).not.toBeNull();
  });

  it('muestra el mensaje de la API ante un rango inválido', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        respuestaJson(422, { title: 'Regla de negocio', detail: 'El rango de fechas no puede ser mayor a un año.', status: 422 }),
      ),
    );
    renderizarApp('/bitacora');

    await userEvent.clear(screen.getByLabelText('Desde'));
    await userEvent.type(screen.getByLabelText('Desde'), '2024-01-01');
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('El rango de fechas no puede ser mayor a un año.');
  });

  it('pagina los resultados', async () => {
    const fetchSimulado = vi
      .fn()
      .mockResolvedValueOnce(respuestaJson(200, { elementos: [entrada], pagina: 1, tamano: 20, total: 45 }))
      .mockResolvedValueOnce(respuestaJson(200, { elementos: [entrada], pagina: 2, tamano: 20, total: 45 }));
    vi.stubGlobal('fetch', fetchSimulado);
    renderizarApp('/bitacora');

    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }));
    expect(await screen.findByText('Página 1 de 3 (45 accesos)')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Siguiente' }));

    expect(await screen.findByText('Página 2 de 3 (45 accesos)')).toBeInTheDocument();
    expect(new URL(fetchSimulado.mock.calls[1][0] as string).searchParams.get('pagina')).toBe('2');
  });

  it('sin el permiso bitacora.consultar no muestra la pantalla ni la opción del menú', () => {
    sessionStorage.clear();
    conSesion(sesionFicticia());

    renderizarApp('/bitacora');

    expect(screen.getByRole('alert')).toHaveTextContent('No tiene permiso para usar esta función.');
    expect(screen.queryByRole('link', { name: 'Bitácora' })).not.toBeInTheDocument();
  });
});
