import { useEffect, useState } from 'react';
import { ErrorApi } from '../api/cliente';
import { consultarMatrizPermisos, type MatrizPermisos as Matriz } from '../api/permisos';

/**
 * Matriz fija de roles y permisos, de solo lectura (FR-013). Los roles y sus permisos solo cambian con
 * una nueva versión del sistema.
 */
export function MatrizPermisos() {
  const [matriz, setMatriz] = useState<Matriz | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    consultarMatrizPermisos()
      .then(setMatriz)
      .catch((e: unknown) =>
        setError(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo conectar con el sistema.'),
      );
  }, []);

  return (
    <main className="contenido">
      <h1>Matriz de permisos</h1>
      <p>Los roles y sus permisos son fijos: solo cambian con una nueva versión del sistema.</p>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {matriz && (
        <table>
          <thead>
            <tr>
              <th>Permiso</th>
              {matriz.roles.map((r) => (
                <th key={r.codigo}>{r.nombre}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {matriz.permisos.map((p) => (
              <tr key={p.codigo}>
                <th scope="row">{p.descripcion}</th>
                {matriz.roles.map((r) => (
                  <td key={r.codigo} aria-label={p.roles.includes(r.codigo) ? 'Sí' : 'No'}>
                    {p.roles.includes(r.codigo) ? '✔' : ''}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
