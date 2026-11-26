// Sesión del usuario guardada en sessionStorage: se borra al cerrar la pestaña (research D9).

export interface UsuarioSesion {
  id: string;
  nombreUsuario: string;
  nombreCompleto: string;
  roles: string[];
  permisos: string[];
  cambioContrasenaPendiente: boolean;
}

/** Respuesta SesionRespuesta del contrato de la API. */
export interface Sesion {
  token: string;
  expiraEn: string;
  usuario: UsuarioSesion;
}

const CLAVE = 'sigh.sesion';

export function leerSesion(): Sesion | null {
  const texto = sessionStorage.getItem(CLAVE);
  if (!texto) return null;
  try {
    const sesion = JSON.parse(texto) as Sesion;
    if (new Date(sesion.expiraEn).getTime() <= Date.now()) {
      borrarSesion();
      return null;
    }
    return sesion;
  } catch {
    borrarSesion();
    return null;
  }
}

export function guardarSesion(sesion: Sesion): void {
  sessionStorage.setItem(CLAVE, JSON.stringify(sesion));
}

export function borrarSesion(): void {
  sessionStorage.removeItem(CLAVE);
}
