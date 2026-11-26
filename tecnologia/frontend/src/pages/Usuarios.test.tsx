import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Usuario } from '../api/usuarios';
import { conSesion, renderizarApp, respuestaJson, sesionFicticia } from '../pruebas/utilidades';

const medico: Usuario = {
  id: '00000000-0000-0000-0000-0000000000aa',
  nombreUsuario: 'med01',
  nombreCompleto: 'Médico Ficticio',
  estado: 'Activo',
  roles: ['Medico'],
  cambioContrasenaPendiente: false,
  bloqueado: false,
  creadoEn: '2026-11-26T08:00:00Z',
  ultimoAcceso: null,
};

type Llamada = { url: string; metodo: string; cuerpo: unknown };

/** Simula la API de usuarios y guarda cada llamada. */
function simularApi() {
  const llamadas: Llamada[] = [];
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string, opciones: RequestInit) => {
      const metodo = opciones.method ?? 'GET';
      llamadas.push({ url, metodo, cuerpo: opciones.body ? JSON.parse(String(opciones.body)) : undefined });
      if (metodo === 'GET') return respuestaJson(200, { elementos: [medico], pagina: 1, tamano: 20, total: 1 });
      if (metodo === 'POST' && url.endsWith('/api/v1/usuarios'))
        return respuestaJson(201, { ...medico, id: 'nuevo', nombreUsuario: 'farm01', roles: ['Farmaceutico'] });
      return new Response(null, { status: 204 });
    }),
  );
  return llamadas;
}

describe('Usuarios', () => {
  beforeEach(() => {
    sessionStorage.clear();
    conSesion(
      sesionFicticia({
        nombreCompleto: 'Administrador Ficticio',
        roles: ['Administrador'],
        permisos: ['usuarios.gestionar', 'bitacora.consultar', 'notificaciones.recibir'],
      }),
    );
  });
  afterEach(() => vi.unstubAllGlobals());

  it('al abrir la ruta directamente, la primera petición ya lleva el token de la sesión', async () => {
    simularApi();

    renderizarApp('/usuarios');
    await screen.findByText('med01');

    const [, opciones] = vi.mocked(fetch).mock.calls[0];
    expect((opciones?.headers as Record<string, string>).Authorization).toBe('Bearer token-de-prueba');
  });

  it('lista los usuarios con sus roles y no ofrece eliminar', async () => {
    simularApi();

    renderizarApp('/usuarios');

    const fila = (await screen.findByText('med01')).closest('tr')!;
    expect(within(fila).getByText('Médico')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /eliminar/i })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Usuarios' })).toBeInTheDocument();
  });

  it('crea un usuario con sus roles', async () => {
    const llamadas = simularApi();
    renderizarApp('/usuarios');
    await screen.findByText('med01');

    await userEvent.click(screen.getByRole('button', { name: 'Nuevo usuario' }));
    const formulario = screen.getByRole('form', { name: 'Nuevo usuario' });
    await userEvent.type(within(formulario).getByLabelText('Nombre de usuario'), 'farm01');
    await userEvent.type(within(formulario).getByLabelText('Nombre completo'), 'Farmacéutico Ficticio');
    await userEvent.type(within(formulario).getByLabelText('Contraseña inicial'), 'Inicial2026');
    await userEvent.click(within(formulario).getByLabelText('Farmacéutico'));
    await userEvent.click(within(formulario).getByRole('button', { name: 'Guardar' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Se creó farm01');
    const alta = llamadas.find((l) => l.metodo === 'POST' && l.url.endsWith('/api/v1/usuarios'));
    expect(alta?.cuerpo).toEqual({
      nombreUsuario: 'farm01',
      nombreCompleto: 'Farmacéutico Ficticio',
      contrasenaInicial: 'Inicial2026',
      roles: ['Farmaceutico'],
    });
  });

  it('no deja guardar un usuario sin roles', async () => {
    const llamadas = simularApi();
    renderizarApp('/usuarios');
    await screen.findByText('med01');

    await userEvent.click(screen.getByRole('button', { name: 'Nuevo usuario' }));
    const formulario = screen.getByRole('form', { name: 'Nuevo usuario' });
    await userEvent.type(within(formulario).getByLabelText('Nombre de usuario'), 'farm01');
    await userEvent.type(within(formulario).getByLabelText('Nombre completo'), 'Farmacéutico Ficticio');
    await userEvent.type(within(formulario).getByLabelText('Contraseña inicial'), 'Inicial2026');
    await userEvent.click(within(formulario).getByRole('button', { name: 'Guardar' }));

    expect(within(formulario).getByRole('alert')).toHaveTextContent('Debe asignar al menos un rol.');
    expect(llamadas.some((l) => l.metodo === 'POST')).toBe(false);
  });

  it('desactiva un usuario', async () => {
    const llamadas = simularApi();
    renderizarApp('/usuarios');
    const fila = (await screen.findByText('med01')).closest('tr')!;

    await userEvent.click(within(fila).getByRole('button', { name: 'Desactivar' }));

    await waitFor(() =>
      expect(llamadas.some((l) => l.metodo === 'POST' && l.url.endsWith(`/usuarios/${medico.id}/desactivar`))).toBe(true),
    );
    expect(await screen.findByRole('status')).toHaveTextContent('Se desactivó med01.');
  });

  it('edita el nombre y los roles con una llamada a cada endpoint', async () => {
    const llamadas = simularApi();
    renderizarApp('/usuarios');
    const fila = (await screen.findByText('med01')).closest('tr')!;

    await userEvent.click(within(fila).getByRole('button', { name: 'Editar' }));
    const formulario = screen.getByRole('form', { name: 'Editar usuario' });
    const nombre = within(formulario).getByLabelText('Nombre completo');
    await userEvent.clear(nombre);
    await userEvent.type(nombre, 'Nombre Corregido');
    await userEvent.click(within(formulario).getByLabelText('Epidemiólogo'));
    await userEvent.click(within(formulario).getByRole('button', { name: 'Guardar' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Se actualizó med01');
    const edicion = llamadas.find((l) => l.metodo === 'PUT' && l.url.endsWith(`/usuarios/${medico.id}`));
    const roles = llamadas.find((l) => l.metodo === 'PUT' && l.url.endsWith(`/usuarios/${medico.id}/roles`));
    expect(edicion?.cuerpo).toEqual({ nombreCompleto: 'Nombre Corregido' });
    expect(roles?.cuerpo).toEqual({ roles: ['Medico', 'Epidemiologo'] });
  });

  it('restablece la contraseña con una temporal', async () => {
    const llamadas = simularApi();
    renderizarApp('/usuarios');
    const fila = (await screen.findByText('med01')).closest('tr')!;

    await userEvent.click(within(fila).getByRole('button', { name: 'Restablecer contraseña' }));
    const formulario = screen.getByRole('form', { name: 'Restablecer contraseña' });
    await userEvent.type(within(formulario).getByLabelText('Contraseña temporal'), 'Temporal2026');
    await userEvent.click(within(formulario).getByRole('button', { name: 'Restablecer' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Se restableció la contraseña de med01');
    const restablecer = llamadas.find((l) => l.url.endsWith(`/usuarios/${medico.id}/restablecer-contrasena`));
    expect(restablecer?.cuerpo).toEqual({ contrasenaTemporal: 'Temporal2026' });
  });

  it('busca y filtra por rol y estado', async () => {
    const llamadas = simularApi();
    renderizarApp('/usuarios');
    await screen.findByText('med01');

    await userEvent.type(screen.getByLabelText('Buscar'), 'med');
    await userEvent.selectOptions(screen.getByLabelText('Rol'), 'Medico');
    await userEvent.selectOptions(screen.getByLabelText('Estado'), 'Activo');
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }));

    const ultima = new URL(llamadas.filter((l) => l.metodo === 'GET').at(-1)!.url);
    expect(ultima.searchParams.get('busqueda')).toBe('med');
    expect(ultima.searchParams.get('rol')).toBe('Medico');
    expect(ultima.searchParams.get('estado')).toBe('Activo');
  });

  it('sin el permiso no muestra la pantalla ni la opción del menú', () => {
    sessionStorage.clear();
    conSesion(sesionFicticia());
    simularApi();

    renderizarApp('/usuarios');

    expect(screen.getByRole('alert')).toHaveTextContent('No tiene permiso para usar esta función.');
    expect(screen.queryByRole('link', { name: 'Usuarios' })).not.toBeInTheDocument();
  });
});
