// Llamadas a /api/v1/usuarios (contrato api-administracion, HU14).
import { solicitar } from './cliente';

export const ROLES = [
  { codigo: 'Administrador', nombre: 'Administrador' },
  { codigo: 'PersonalAdmision', nombre: 'Personal de Admisión' },
  { codigo: 'EnfermeroTriaje', nombre: 'Enfermero(a) de Triaje' },
  { codigo: 'Medico', nombre: 'Médico' },
  { codigo: 'Farmaceutico', nombre: 'Farmacéutico' },
  { codigo: 'PersonalLaboratorio', nombre: 'Personal de Laboratorio' },
  { codigo: 'Epidemiologo', nombre: 'Epidemiólogo' },
  { codigo: 'Directivo', nombre: 'Directivo' },
] as const;

export type CodigoRol = (typeof ROLES)[number]['codigo'];
export type EstadoUsuario = 'Activo' | 'Desactivado';

export function nombreRol(codigo: string): string {
  return ROLES.find((r) => r.codigo === codigo)?.nombre ?? codigo;
}

export interface Usuario {
  id: string;
  nombreUsuario: string;
  nombreCompleto: string;
  estado: EstadoUsuario;
  roles: CodigoRol[];
  cambioContrasenaPendiente: boolean;
  bloqueado: boolean;
  creadoEn: string;
  ultimoAcceso: string | null;
}

export interface Pagina<T> {
  elementos: T[];
  pagina: number;
  tamano: number;
  total: number;
}

export interface FiltroUsuarios {
  busqueda?: string;
  rol?: CodigoRol | '';
  estado?: EstadoUsuario | '';
  pagina: number;
  tamano: number;
}

const RUTA = '/api/v1/usuarios';

export function listarUsuarios(filtro: FiltroUsuarios): Promise<Pagina<Usuario>> {
  const parametros = new URLSearchParams({ pagina: String(filtro.pagina), tamano: String(filtro.tamano) });
  if (filtro.busqueda?.trim()) parametros.set('busqueda', filtro.busqueda.trim());
  if (filtro.rol) parametros.set('rol', filtro.rol);
  if (filtro.estado) parametros.set('estado', filtro.estado);
  return solicitar<Pagina<Usuario>>(`${RUTA}?${parametros}`);
}

export function crearUsuario(datos: {
  nombreUsuario: string;
  nombreCompleto: string;
  contrasenaInicial: string;
  roles: CodigoRol[];
}): Promise<Usuario> {
  return solicitar<Usuario>(RUTA, { metodo: 'POST', cuerpo: datos });
}

export function editarUsuario(id: string, nombreCompleto: string): Promise<Usuario> {
  return solicitar<Usuario>(`${RUTA}/${id}`, { metodo: 'PUT', cuerpo: { nombreCompleto } });
}

export function cambiarRoles(id: string, roles: CodigoRol[]): Promise<Usuario> {
  return solicitar<Usuario>(`${RUTA}/${id}/roles`, { metodo: 'PUT', cuerpo: { roles } });
}

export function desactivarUsuario(id: string): Promise<void> {
  return solicitar<void>(`${RUTA}/${id}/desactivar`, { metodo: 'POST' });
}

export function reactivarUsuario(id: string): Promise<void> {
  return solicitar<void>(`${RUTA}/${id}/reactivar`, { metodo: 'POST' });
}

export function restablecerContrasena(id: string, contrasenaTemporal: string): Promise<void> {
  return solicitar<void>(`${RUTA}/${id}/restablecer-contrasena`, { metodo: 'POST', cuerpo: { contrasenaTemporal } });
}
