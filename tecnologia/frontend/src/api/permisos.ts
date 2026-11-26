// Llamada a /api/v1/roles/matriz-permisos (contrato MatrizPermisos, FR-013).
import { solicitar } from './cliente';

export interface MatrizPermisos {
  roles: { codigo: string; nombre: string }[];
  permisos: { codigo: string; descripcion: string; roles: string[] }[];
}

export function consultarMatrizPermisos(): Promise<MatrizPermisos> {
  return solicitar<MatrizPermisos>('/api/v1/roles/matriz-permisos');
}
