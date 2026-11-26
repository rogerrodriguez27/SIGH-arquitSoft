// Llamada a /api/v1/bitacora-accesos (contrato PaginaBitacora, HU16).
import { solicitar } from './cliente';
import type { Pagina } from './usuarios';

export const ACCIONES = [
  { codigo: 'CONSULTAR_HISTORIA', nombre: 'Consultar historia' },
  { codigo: 'REGISTRAR_ATENCION', nombre: 'Registrar atención' },
  { codigo: 'AGREGAR_NOTA_CORRECCION', nombre: 'Agregar nota de corrección' },
  { codigo: 'REGISTRAR_RESULTADO', nombre: 'Registrar resultado' },
  { codigo: 'DISPENSAR_RECETA', nombre: 'Dispensar receta' },
  { codigo: 'REGISTRAR_INGRESO', nombre: 'Registrar ingreso' },
  { codigo: 'REGISTRAR_TRIAJE', nombre: 'Registrar triaje' },
] as const;

export function nombreAccion(codigo: string): string {
  return ACCIONES.find((a) => a.codigo === codigo)?.nombre ?? codigo;
}

/** Entrada de la bitácora: sin contenido clínico (FR-023). */
export interface EntradaBitacora {
  id: string;
  usuarioId: string;
  nombreUsuario: string;
  rol: string;
  fechaHora: string;
  accion: string;
  historiaId: string;
  moduloOrigen: string;
  resultado: 'Permitido' | 'Denegado';
}

export interface FiltroBitacora {
  desde: string;
  hasta: string;
  usuarioId?: string;
  historiaId?: string;
  accion?: string;
  resultado?: '' | 'Permitido' | 'Denegado';
  pagina: number;
  tamano: number;
}

export function consultarBitacora(filtro: FiltroBitacora): Promise<Pagina<EntradaBitacora>> {
  const parametros = new URLSearchParams({
    desde: filtro.desde,
    hasta: filtro.hasta,
    pagina: String(filtro.pagina),
    tamano: String(filtro.tamano),
  });
  if (filtro.usuarioId?.trim()) parametros.set('usuarioId', filtro.usuarioId.trim());
  if (filtro.historiaId?.trim()) parametros.set('historiaId', filtro.historiaId.trim());
  if (filtro.accion) parametros.set('accion', filtro.accion);
  if (filtro.resultado) parametros.set('resultado', filtro.resultado);
  return solicitar<Pagina<EntradaBitacora>>(`/api/v1/bitacora-accesos?${parametros}`);
}
