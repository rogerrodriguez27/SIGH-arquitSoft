import { Navigate, Outlet, Route, Routes } from 'react-router';
import { RUTA_CAMBIAR_CONTRASENA, RUTA_INICIAR_SESION, RutaProtegida } from './auth/RutaProtegida';
import { MenuPrincipal } from './components/MenuPrincipal';
import { CambiarContrasena } from './pages/CambiarContrasena';
import { IniciarSesion } from './pages/IniciarSesion';
import { Inicio } from './pages/Inicio';

function Plantilla() {
  return (
    <>
      <MenuPrincipal />
      <Outlet />
    </>
  );
}

/** Rutas de la SPA. Cada historia agrega sus pantallas dentro de la plantilla protegida. */
export function App() {
  return (
    <Routes>
      <Route path={RUTA_INICIAR_SESION} element={<IniciarSesion />} />
      <Route
        element={
          <RutaProtegida>
            <Plantilla />
          </RutaProtegida>
        }
      >
        <Route index element={<Inicio />} />
        <Route path={RUTA_CAMBIAR_CONTRASENA} element={<CambiarContrasena />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
