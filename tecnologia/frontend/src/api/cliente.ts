// Cliente HTTP de la API del SIGH (research D9): agrega el JWT y traduce application/problem+json.

import { borrarSesion, leerSesion } from '../auth/sesion';

export const URL_API: string = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';

/** Error devuelto por la API en formato problem+json (RFC 9457). */
export class ErrorApi extends Error {
  readonly estado: number;
  readonly titulo: string;
  readonly detalle?: string;

  constructor(estado: number, titulo: string, detalle?: string) {
    super(detalle ?? titulo);
    this.estado = estado;
    this.titulo = titulo;
    this.detalle = detalle;
  }
}

interface ConfiguracionCliente {
  obtenerToken: () => string | null;
  alExpirarSesion: () => void;
}

// El token se lee de la sesión guardada desde la primera petición: al recargar o abrir una ruta directa,
// las páginas piden datos antes de que el contexto de sesión termine de configurarse.
let configuracion: ConfiguracionCliente = {
  obtenerToken: () => leerSesion()?.token ?? null,
  alExpirarSesion: () => borrarSesion(),
};

/** Lo llama el contexto de sesión para entregar el token y saber cuándo la sesión dejó de valer. */
export function configurarCliente(nueva: ConfiguracionCliente): void {
  configuracion = nueva;
}

interface OpcionesSolicitud {
  metodo?: 'GET' | 'POST' | 'PUT';
  cuerpo?: unknown;
  /** Para el inicio de sesión: no envía token y un 401 no cierra la sesión. */
  anonimo?: boolean;
}

export async function solicitar<T>(ruta: string, opciones: OpcionesSolicitud = {}): Promise<T> {
  const { metodo = 'GET', cuerpo, anonimo = false } = opciones;
  const cabeceras: Record<string, string> = { Accept: 'application/json' };
  if (cuerpo !== undefined) cabeceras['Content-Type'] = 'application/json';

  const token = anonimo ? null : configuracion.obtenerToken();
  if (token) cabeceras.Authorization = `Bearer ${token}`;

  const respuesta = await fetch(`${URL_API}${ruta}`, {
    method: metodo,
    headers: cabeceras,
    body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
  });

  if (respuesta.status === 401 && !anonimo) {
    configuracion.alExpirarSesion();
  }

  if (!respuesta.ok) {
    throw await leerError(respuesta);
  }

  if (respuesta.status === 204) return undefined as T;
  return (await respuesta.json()) as T;
}

async function leerError(respuesta: Response): Promise<ErrorApi> {
  try {
    const problema = (await respuesta.json()) as { title?: string; detail?: string };
    return new ErrorApi(respuesta.status, problema.title ?? 'Error', problema.detail);
  } catch {
    return new ErrorApi(respuesta.status, 'No se pudo completar la operación.');
  }
}
