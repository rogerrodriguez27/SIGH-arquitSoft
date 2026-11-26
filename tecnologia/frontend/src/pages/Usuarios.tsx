import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import {
  ROLES,
  cambiarRoles,
  crearUsuario,
  desactivarUsuario,
  editarUsuario,
  listarUsuarios,
  nombreRol,
  reactivarUsuario,
  restablecerContrasena,
  type CodigoRol,
  type EstadoUsuario,
  type FiltroUsuarios,
  type Pagina,
  type Usuario,
} from '../api/usuarios';
import { FormularioUsuario, type DatosFormularioUsuario } from '../components/FormularioUsuario';

const TAMANO_PAGINA = 20;

type Edicion = { tipo: 'nuevo' } | { tipo: 'editar'; usuario: Usuario } | { tipo: 'restablecer'; usuario: Usuario } | null;

function mensajeDe(e: unknown): string {
  return e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo conectar con el sistema.';
}

/** Gestión de usuarios del administrador (HU14). No hay opción de eliminar (FR-009). */
export function Usuarios() {
  const [filtro, setFiltro] = useState<FiltroUsuarios>({ busqueda: '', rol: '', estado: '', pagina: 1, tamano: TAMANO_PAGINA });
  const [busqueda, setBusqueda] = useState('');
  const [resultado, setResultado] = useState<Pagina<Usuario> | null>(null);
  const [edicion, setEdicion] = useState<Edicion>(null);
  const [mensaje, setMensaje] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    try {
      setResultado(await listarUsuarios(filtro));
      setError(null);
    } catch (e) {
      setError(mensajeDe(e));
    }
  }, [filtro]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const buscar = (evento: FormEvent) => {
    evento.preventDefault();
    setFiltro((f) => ({ ...f, busqueda, pagina: 1 }));
  };

  const ejecutar = async (accion: () => Promise<unknown>, exito: string) => {
    setMensaje(null);
    setError(null);
    try {
      await accion();
      setMensaje(exito);
      await cargar();
    } catch (e) {
      setError(mensajeDe(e));
    }
  };

  const guardar = async (datos: DatosFormularioUsuario) => {
    if (edicion?.tipo === 'editar') {
      const { usuario } = edicion;
      if (datos.nombreCompleto !== usuario.nombreCompleto) await editarUsuario(usuario.id, datos.nombreCompleto);
      const mismosRoles = [...datos.roles].sort().join() === [...usuario.roles].sort().join();
      if (!mismosRoles) await cambiarRoles(usuario.id, datos.roles);
      setMensaje(`Se actualizó ${usuario.nombreUsuario}. Los roles nuevos aplican en su siguiente sesión.`);
    } else {
      const creado = await crearUsuario(datos);
      setMensaje(`Se creó ${creado.nombreUsuario}. Deberá cambiar su contraseña al iniciar sesión.`);
    }
    setEdicion(null);
    await cargar();
  };

  const totalPaginas = resultado ? Math.max(1, Math.ceil(resultado.total / resultado.tamano)) : 1;

  return (
    <main className="contenido">
      <h1>Usuarios</h1>

      <form onSubmit={buscar} className="filtros" aria-label="Filtros de usuarios">
        <label>
          Buscar
          <input
            placeholder="Nombre o usuario"
            maxLength={100}
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />
        </label>
        <label>
          Rol
          <select
            value={filtro.rol}
            onChange={(e) => setFiltro((f) => ({ ...f, rol: e.target.value as CodigoRol | '', pagina: 1 }))}
          >
            <option value="">Todos</option>
            {ROLES.map((r) => (
              <option key={r.codigo} value={r.codigo}>
                {r.nombre}
              </option>
            ))}
          </select>
        </label>
        <label>
          Estado
          <select
            value={filtro.estado}
            onChange={(e) => setFiltro((f) => ({ ...f, estado: e.target.value as EstadoUsuario | '', pagina: 1 }))}
          >
            <option value="">Todos</option>
            <option value="Activo">Activo</option>
            <option value="Desactivado">Desactivado</option>
          </select>
        </label>
        <button type="submit">Buscar</button>
        <button type="button" onClick={() => setEdicion({ tipo: 'nuevo' })}>
          Nuevo usuario
        </button>
      </form>

      {mensaje && (
        <p role="status" className="aviso">
          {mensaje}
        </p>
      )}
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}

      {(edicion?.tipo === 'nuevo' || edicion?.tipo === 'editar') && (
        <FormularioUsuario
          key={edicion.tipo === 'editar' ? edicion.usuario.id : 'nuevo'}
          usuario={edicion.tipo === 'editar' ? edicion.usuario : undefined}
          alGuardar={guardar}
          alCancelar={() => setEdicion(null)}
        />
      )}

      {edicion?.tipo === 'restablecer' && (
        <FormularioRestablecer
          usuario={edicion.usuario}
          alGuardar={async (temporal) => {
            await restablecerContrasena(edicion.usuario.id, temporal);
            setEdicion(null);
            setMensaje(`Se restableció la contraseña de ${edicion.usuario.nombreUsuario}. Deberá cambiarla al iniciar sesión.`);
            await cargar();
          }}
          alCancelar={() => setEdicion(null)}
        />
      )}

      <table>
        <thead>
          <tr>
            <th>Usuario</th>
            <th>Nombre completo</th>
            <th>Roles</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {resultado?.elementos.map((u) => (
            <tr key={u.id}>
              <td>{u.nombreUsuario}</td>
              <td>{u.nombreCompleto}</td>
              <td>{u.roles.map(nombreRol).join(', ')}</td>
              <td>
                {u.estado}
                {u.bloqueado && ' (bloqueado)'}
              </td>
              <td className="acciones">
                <button type="button" onClick={() => setEdicion({ tipo: 'editar', usuario: u })}>
                  Editar
                </button>
                {u.estado === 'Activo' ? (
                  <button
                    type="button"
                    onClick={() => ejecutar(() => desactivarUsuario(u.id), `Se desactivó ${u.nombreUsuario}.`)}
                  >
                    Desactivar
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={() => ejecutar(() => reactivarUsuario(u.id), `Se reactivó ${u.nombreUsuario}.`)}
                  >
                    Reactivar
                  </button>
                )}
                <button type="button" onClick={() => setEdicion({ tipo: 'restablecer', usuario: u })}>
                  Restablecer contraseña
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {resultado && resultado.total === 0 && <p>No hay usuarios con esos filtros.</p>}

      {resultado && resultado.total > 0 && (
        <nav aria-label="Paginación" className="paginacion">
          <button
            type="button"
            disabled={filtro.pagina <= 1}
            onClick={() => setFiltro((f) => ({ ...f, pagina: f.pagina - 1 }))}
          >
            Anterior
          </button>
          <span>
            Página {filtro.pagina} de {totalPaginas} ({resultado.total} usuarios)
          </span>
          <button
            type="button"
            disabled={filtro.pagina >= totalPaginas}
            onClick={() => setFiltro((f) => ({ ...f, pagina: f.pagina + 1 }))}
          >
            Siguiente
          </button>
        </nav>
      )}
    </main>
  );
}

function FormularioRestablecer({
  usuario,
  alGuardar,
  alCancelar,
}: {
  usuario: Usuario;
  alGuardar: (temporal: string) => Promise<void>;
  alCancelar: () => void;
}) {
  const [temporal, setTemporal] = useState('');
  const [error, setError] = useState<string | null>(null);

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setError(null);
    try {
      await alGuardar(temporal);
    } catch (e) {
      setError(mensajeDe(e));
    }
  };

  return (
    <form onSubmit={enviar} aria-label="Restablecer contraseña" className="panel">
      <h2>Restablecer contraseña de {usuario.nombreUsuario}</h2>
      <label>
        Contraseña temporal
        <input
          type="password"
          autoComplete="new-password"
          required
          minLength={8}
          maxLength={128}
          value={temporal}
          onChange={(e) => setTemporal(e.target.value)}
        />
      </label>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      <div className="acciones">
        <button type="submit">Restablecer</button>
        <button type="button" onClick={alCancelar}>
          Cancelar
        </button>
      </div>
    </form>
  );
}
