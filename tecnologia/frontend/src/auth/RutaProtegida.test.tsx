import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { conSesion, renderizarApp, sesionFicticia } from '../pruebas/utilidades';
import { ProveedorSesion } from './ContextoSesion';
import { RutaProtegida } from './RutaProtegida';

describe('RutaProtegida', () => {
  beforeEach(() => sessionStorage.clear());

  it('sin sesión lleva al inicio de sesión', () => {
    renderizarApp('/');

    expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it('con una sesión vencida lleva al inicio de sesión', () => {
    conSesion({ ...sesionFicticia(), expiraEn: new Date(Date.now() - 1000).toISOString() });

    renderizarApp('/');

    expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(sessionStorage.getItem('sigh.sesion')).toBeNull();
  });

  it('con cambio de contraseña pendiente solo permite la pantalla de cambio', () => {
    conSesion(sesionFicticia({ cambioContrasenaPendiente: true }));

    renderizarApp('/');

    expect(screen.getByRole('heading', { name: 'Cambiar contraseña' })).toBeInTheDocument();
  });

  it('con sesión vigente muestra la pantalla principal y el menú', () => {
    conSesion(sesionFicticia());

    renderizarApp('/');

    expect(screen.getByRole('heading', { name: /Bienvenido\(a\)/ })).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Menú principal' })).toBeInTheDocument();
  });

  it('sin el permiso exigido muestra un aviso', () => {
    conSesion(sesionFicticia({ permisos: ['notificaciones.recibir'] }));

    render(
      <MemoryRouter initialEntries={['/usuarios']}>
        <ProveedorSesion>
          <Routes>
            <Route
              path="/usuarios"
              element={
                <RutaProtegida permiso="usuarios.gestionar">
                  <h1>Usuarios</h1>
                </RutaProtegida>
              }
            />
          </Routes>
        </ProveedorSesion>
      </MemoryRouter>,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('No tiene permiso para usar esta función.');
    expect(screen.queryByRole('heading', { name: 'Usuarios' })).not.toBeInTheDocument();
  });
});
