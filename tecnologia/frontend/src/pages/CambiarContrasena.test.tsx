import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { conSesion, renderizarApp, respuestaJson, sesionFicticia } from '../pruebas/utilidades';

describe('Cambiar contraseña', () => {
  beforeEach(() => {
    sessionStorage.clear();
    conSesion(sesionFicticia({ cambioContrasenaPendiente: true }));
  });
  afterEach(() => vi.unstubAllGlobals());

  async function completar(actual: string, nueva: string, confirmacion: string) {
    renderizarApp('/cambiar-contrasena');
    await userEvent.type(screen.getByLabelText('Contraseña actual'), actual);
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), nueva);
    await userEvent.type(screen.getByLabelText('Confirmar contraseña nueva'), confirmacion);
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
  }

  it('no envía nada si la confirmación no coincide', async () => {
    const fetchSimulado = vi.fn();
    vi.stubGlobal('fetch', fetchSimulado);

    await completar('Inicial2026', 'Nueva2026x', 'Otra2026x');

    expect(screen.getByRole('alert')).toHaveTextContent('La confirmación no coincide con la contraseña nueva.');
    expect(fetchSimulado).not.toHaveBeenCalled();
  });

  it('con éxito guarda la sesión nueva sin el cambio pendiente y lleva a la pantalla principal', async () => {
    const fetchSimulado = vi.fn().mockResolvedValue(respuestaJson(200, sesionFicticia()));
    vi.stubGlobal('fetch', fetchSimulado);

    await completar('Inicial2026', 'Nueva2026x', 'Nueva2026x');

    expect(await screen.findByRole('heading', { name: /Bienvenido\(a\)/ })).toBeInTheDocument();
    const [url, opciones] = fetchSimulado.mock.calls[0];
    expect(url).toMatch(/\/api\/v1\/autenticacion\/cambiar-contrasena$/);
    expect(JSON.parse(opciones.body)).toEqual({ contrasenaActual: 'Inicial2026', contrasenaNueva: 'Nueva2026x' });
    expect(JSON.parse(sessionStorage.getItem('sigh.sesion')!).usuario.cambioContrasenaPendiente).toBe(false);
  });

  it('muestra el mensaje de la API si la contraseña actual es incorrecta', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        respuestaJson(422, { title: 'Regla de negocio', detail: 'La contraseña actual no es correcta.', status: 422 }),
      ),
    );

    await completar('Otra2026x', 'Nueva2026x', 'Nueva2026x');

    expect(await screen.findByRole('alert')).toHaveTextContent('La contraseña actual no es correcta.');
  });
});
