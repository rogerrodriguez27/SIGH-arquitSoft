import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { ACCIONES, consultarBitacora, nombreAccion, type EntradaBitacora, type FiltroBitacora } from '../api/bitacora';
import { nombreRol, type Pagina } from '../api/usuarios';

const TAMANO_PAGINA = 20;

/** Fecha local en formato yyyy-mm-dd para los campos de tipo date. */
function fechaLocal(fecha: Date): string {
  const desplazada = new Date(fecha.getTime() - fecha.getTimezoneOffset() * 60_000);
  return desplazada.toISOString().slice(0, 10);
}

/** Convierte el rango de días locales en instantes: desde el inicio del primer día hasta el fin del último. */
function rangoIso(desde: string, hasta: string): { desde: string; hasta: string } {
  return {
    desde: new Date(`${desde}T00:00:00`).toISOString(),
    hasta: new Date(`${hasta}T23:59:59.999`).toISOString(),
  };
}

/**
 * Consulta de la bitácora de accesos a la HCE (HU16). Solo muestra quién, cuándo, qué acción, sobre
 * qué historia y con qué resultado: nunca contenido clínico (FR-023). Cada consulta queda registrada.
 */
export function Bitacora() {
  const hoy = fechaLocal(new Date());
  const [desde, setDesde] = useState(hoy);
  const [hasta, setHasta] = useState(hoy);
  const [usuarioId, setUsuarioId] = useState('');
  const [historiaId, setHistoriaId] = useState('');
  const [accion, setAccion] = useState('');
  const [resultado, setResultado] = useState<FiltroBitacora['resultado']>('');
  const [filtroActual, setFiltroActual] = useState<FiltroBitacora | null>(null);
  const [pagina, setPagina] = useState<Pagina<EntradaBitacora> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [cargando, setCargando] = useState(false);

  const cargar = async (filtro: FiltroBitacora) => {
    setError(null);
    setCargando(true);
    try {
      setPagina(await consultarBitacora(filtro));
      setFiltroActual(filtro);
    } catch (e) {
      setPagina(null);
      setError(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo conectar con el sistema.');
    } finally {
      setCargando(false);
    }
  };

  const buscar = (evento: FormEvent) => {
    evento.preventDefault();
    void cargar({ ...rangoIso(desde, hasta), usuarioId, historiaId, accion, resultado, pagina: 1, tamano: TAMANO_PAGINA });
  };

  const irA = (numero: number) => {
    if (filtroActual) void cargar({ ...filtroActual, pagina: numero });
  };

  const totalPaginas = pagina ? Math.max(1, Math.ceil(pagina.total / pagina.tamano)) : 1;

  return (
    <main className="contenido">
      <h1>Bitácora de accesos a la HCE</h1>
      <p>Cada consulta a la bitácora también queda registrada. El rango máximo es de un año.</p>

      <form onSubmit={buscar} className="filtros" aria-label="Filtros de la bitácora">
        <label>
          Desde
          <input type="date" required value={desde} onChange={(e) => setDesde(e.target.value)} />
        </label>
        <label>
          Hasta
          <input type="date" required value={hasta} onChange={(e) => setHasta(e.target.value)} />
        </label>
        <label>
          ID de usuario
          <input
            placeholder="Opcional"
            pattern="[0-9a-fA-F-]{36}"
            value={usuarioId}
            onChange={(e) => setUsuarioId(e.target.value)}
          />
        </label>
        <label>
          ID de historia clínica
          <input
            placeholder="Opcional"
            pattern="[0-9a-fA-F-]{36}"
            value={historiaId}
            onChange={(e) => setHistoriaId(e.target.value)}
          />
        </label>
        <label>
          Acción
          <select value={accion} onChange={(e) => setAccion(e.target.value)}>
            <option value="">Todas</option>
            {ACCIONES.map((a) => (
              <option key={a.codigo} value={a.codigo}>
                {a.nombre}
              </option>
            ))}
          </select>
        </label>
        <label>
          Resultado
          <select value={resultado} onChange={(e) => setResultado(e.target.value as FiltroBitacora['resultado'])}>
            <option value="">Todos</option>
            <option value="Permitido">Permitido</option>
            <option value="Denegado">Denegado</option>
          </select>
        </label>
        <button type="submit" disabled={cargando}>
          {cargando ? 'Buscando…' : 'Buscar'}
        </button>
      </form>

      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}

      {pagina && (
        <>
          <table>
            <thead>
              <tr>
                <th>Fecha y hora</th>
                <th>Usuario</th>
                <th>Rol</th>
                <th>Acción</th>
                <th>Historia clínica</th>
                <th>Módulo</th>
                <th>Resultado</th>
              </tr>
            </thead>
            <tbody>
              {pagina.elementos.map((e) => (
                <tr key={e.id}>
                  <td>{new Date(e.fechaHora).toLocaleString('es-PE')}</td>
                  <td>{e.nombreUsuario}</td>
                  <td>{nombreRol(e.rol)}</td>
                  <td>{nombreAccion(e.accion)}</td>
                  <td>{e.historiaId}</td>
                  <td>{e.moduloOrigen}</td>
                  <td>{e.resultado}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {pagina.total === 0 && <p>No hay accesos con esos filtros.</p>}
          {pagina.total > 0 && (
            <nav aria-label="Paginación" className="paginacion">
              <button type="button" disabled={pagina.pagina <= 1 || cargando} onClick={() => irA(pagina.pagina - 1)}>
                Anterior
              </button>
              <span>
                Página {pagina.pagina} de {totalPaginas} ({pagina.total} accesos)
              </span>
              <button
                type="button"
                disabled={pagina.pagina >= totalPaginas || cargando}
                onClick={() => irA(pagina.pagina + 1)}
              >
                Siguiente
              </button>
            </nav>
          )}
        </>
      )}
    </main>
  );
}
