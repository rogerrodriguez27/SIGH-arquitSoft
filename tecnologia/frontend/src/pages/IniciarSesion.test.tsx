import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderizarApp, respuestaJson, sesionFicticia } from '../pruebas/utilidades';

describe('Iniciar sesión', () => {
  beforeEach(() => sessionStorage.clear());
  afterEach(() => vi.unstubAllGlobals());

  async function ingresar(usuario: string, contrasena: string) {
    renderizarApp('/iniciar-sesion');
    await userEvent.type(screen.getByLabelText('Usuario'), usuario);
    await userEvent.type(screen.getByLabelText('Contraseña'), contrasena);
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));
  }

  it('con credenciales válidas lleva a la pantalla principal y guarda la sesión', async () => {
    const fetchSimulado = vi.fn().mockResolvedValue(respuestaJson(200, sesionFicticia()));
    vi.stubGlobal('fetch', fetchSimulado);

    await ingresar('med01', 'Clave2026');

    expect(await screen.findByRole('heading', { name: /Bienvenido\(a\), Médico Ficticio/ })).toBeInTheDocument();
    expect(sessionStorage.getItem('sigh.sesion')).toContain('token-de-prueba');
    const [url, opciones] = fetchSimulado.mock.calls[0];
    expect(url).toMatch(/\/api\/v1\/autenticacion\/iniciar-sesion$/);
    expect(opciones.headers.Authorization).toBeUndefined();
  });

  it('con credenciales inválidas muestra el mensaje genérico', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(respuestaJson(401, { title: 'Usuario o contraseña incorrectos', status: 401 })),
    );

    await ingresar('farm01', 'Incorrecta1');

    expect(await screen.findByRole('alert')).toHaveTextContent('Usuario o contraseña incorrectos');
    expect(sessionStorage.getItem('sigh.sesion')).toBeNull();
  });

  it('con cambio de contraseña pendiente lleva directo a cambiar la contraseña', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(respuestaJson(200, sesionFicticia({ cambioContrasenaPendiente: true }))),
    );

    await ingresar('admin', 'Inicial2026');

    expect(await screen.findByRole('heading', { name: 'Cambiar contraseña' })).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Debe cambiar su contraseña antes de continuar.');
  });

  it('cerrar sesión descarta el token y vuelve al inicio de sesión', async () => {
    sessionStorage.setItem('sigh.sesion', JSON.stringify(sesionFicticia()));
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    renderizarApp('/');
    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));

    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(sessionStorage.getItem('sigh.sesion')).toBeNull();
  });
});
