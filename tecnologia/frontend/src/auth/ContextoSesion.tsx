import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { configurarCliente, solicitar } from '../api/cliente';
import { borrarSesion, guardarSesion, leerSesion, type Sesion } from './sesion';

interface ValorContextoSesion {
  sesion: Sesion | null;
  iniciarSesion: (nombreUsuario: string, contrasena: string) => Promise<Sesion>;
  cambiarContrasena: (contrasenaActual: string, contrasenaNueva: string) => Promise<Sesion>;
  cerrarSesion: () => Promise<void>;
  tienePermiso: (permiso: string) => boolean;
}

const ContextoSesion = createContext<ValorContextoSesion | null>(null);

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<Sesion | null>(() => leerSesion());

  const establecer = useCallback((nueva: Sesion | null) => {
    if (nueva) guardarSesion(nueva);
    else borrarSesion();
    setSesion(nueva);
  }, []);

  // El cliente HTTP usa el token vigente; ante un 401 la sesión se descarta (FR-002, FR-007).
  useEffect(() => {
    configurarCliente({
      obtenerToken: () => leerSesion()?.token ?? null,
      alExpirarSesion: () => establecer(null),
    });
  }, [establecer]);

  // La sesión dura 30 minutos: al vencer, se descarta y se vuelve al inicio de sesión.
  useEffect(() => {
    if (!sesion) return;
    const restante = new Date(sesion.expiraEn).getTime() - Date.now();
    const temporizador = setTimeout(() => establecer(null), Math.max(restante, 0));
    return () => clearTimeout(temporizador);
  }, [sesion, establecer]);

  const iniciarSesion = useCallback(
    async (nombreUsuario: string, contrasena: string) => {
      const nueva = await solicitar<Sesion>('/api/v1/autenticacion/iniciar-sesion', {
        metodo: 'POST',
        cuerpo: { nombreUsuario, contrasena },
        anonimo: true,
      });
      establecer(nueva);
      return nueva;
    },
    [establecer],
  );

  const cambiarContrasena = useCallback(
    async (contrasenaActual: string, contrasenaNueva: string) => {
      const nueva = await solicitar<Sesion>('/api/v1/autenticacion/cambiar-contrasena', {
        metodo: 'POST',
        cuerpo: { contrasenaActual, contrasenaNueva },
      });
      establecer(nueva);
      return nueva;
    },
    [establecer],
  );

  const cerrarSesion = useCallback(async () => {
    try {
      await solicitar<void>('/api/v1/autenticacion/cerrar-sesion', { metodo: 'POST' });
    } catch {
      // La API no guarda sesiones (DA07): aunque falle la llamada, el token se descarta igual.
    } finally {
      establecer(null);
    }
  }, [establecer]);

  const valor = useMemo<ValorContextoSesion>(
    () => ({
      sesion,
      iniciarSesion,
      cambiarContrasena,
      cerrarSesion,
      tienePermiso: (permiso) => sesion?.usuario.permisos.includes(permiso) ?? false,
    }),
    [sesion, iniciarSesion, cambiarContrasena, cerrarSesion],
  );

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>;
}

export function useSesion(): ValorContextoSesion {
  const valor = useContext(ContextoSesion);
  if (!valor) throw new Error('useSesion debe usarse dentro de ProveedorSesion.');
  return valor;
}
