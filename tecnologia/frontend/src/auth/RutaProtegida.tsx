import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useSesion } from './ContextoSesion';

export const RUTA_INICIAR_SESION = '/iniciar-sesion';
export const RUTA_CAMBIAR_CONTRASENA = '/cambiar-contrasena';

interface Props {
  children: ReactNode;
  /** Permiso necesario; si falta, se muestra un aviso. La seguridad real está en la API (research D9). */
  permiso?: string;
}

/**
 * Exige una sesión vigente. Con el cambio de contraseña pendiente, solo deja entrar a la pantalla de
 * cambio de contraseña (FR-006).
 */
export function RutaProtegida({ children, permiso }: Props) {
  const { sesion, tienePermiso } = useSesion();
  const ubicacion = useLocation();

  if (!sesion) {
    return <Navigate to={RUTA_INICIAR_SESION} replace state={{ desde: ubicacion.pathname }} />;
  }

  if (sesion.usuario.cambioContrasenaPendiente && ubicacion.pathname !== RUTA_CAMBIAR_CONTRASENA) {
    return <Navigate to={RUTA_CAMBIAR_CONTRASENA} replace />;
  }

  if (permiso && !tienePermiso(permiso)) {
    return (
      <p role="alert" className="aviso">
        No tiene permiso para usar esta función.
      </p>
    );
  }

  return <>{children}</>;
}
