import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { ROLES, type CodigoRol, type Usuario } from '../api/usuarios';

export interface DatosFormularioUsuario {
  nombreUsuario: string;
  nombreCompleto: string;
  contrasenaInicial: string;
  roles: CodigoRol[];
}

interface Props {
  /** Usuario a editar; si no se indica, el formulario es de alta. */
  usuario?: Usuario;
  alGuardar: (datos: DatosFormularioUsuario) => Promise<void>;
  alCancelar: () => void;
}

/** Alta y edición de usuarios con sus roles (FR-008, FR-010). */
export function FormularioUsuario({ usuario, alGuardar, alCancelar }: Props) {
  const esEdicion = usuario !== undefined;
  const [nombreUsuario, setNombreUsuario] = useState(usuario?.nombreUsuario ?? '');
  const [nombreCompleto, setNombreCompleto] = useState(usuario?.nombreCompleto ?? '');
  const [contrasenaInicial, setContrasenaInicial] = useState('');
  const [roles, setRoles] = useState<CodigoRol[]>(usuario?.roles ?? []);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const alternarRol = (codigo: CodigoRol) =>
    setRoles((actuales) => (actuales.includes(codigo) ? actuales.filter((r) => r !== codigo) : [...actuales, codigo]));

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setError(null);
    if (roles.length === 0) {
      setError('Debe asignar al menos un rol.');
      return;
    }
    setEnviando(true);
    try {
      await alGuardar({ nombreUsuario, nombreCompleto, contrasenaInicial, roles });
    } catch (e) {
      setError(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo conectar con el sistema.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <form onSubmit={enviar} aria-label={esEdicion ? 'Editar usuario' : 'Nuevo usuario'} className="panel">
      <h2>{esEdicion ? `Editar ${usuario.nombreUsuario}` : 'Nuevo usuario'}</h2>
      {!esEdicion && (
        <label>
          Nombre de usuario
          <input
            required
            minLength={3}
            maxLength={50}
            value={nombreUsuario}
            onChange={(e) => setNombreUsuario(e.target.value)}
          />
        </label>
      )}
      <label>
        Nombre completo
        <input
          required
          minLength={3}
          maxLength={150}
          value={nombreCompleto}
          onChange={(e) => setNombreCompleto(e.target.value)}
        />
      </label>
      {!esEdicion && (
        <label>
          Contraseña inicial
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            maxLength={128}
            value={contrasenaInicial}
            onChange={(e) => setContrasenaInicial(e.target.value)}
          />
        </label>
      )}
      <fieldset>
        <legend>Roles</legend>
        {ROLES.map((rol) => (
          <label key={rol.codigo} className="opcion">
            <input
              type="checkbox"
              checked={roles.includes(rol.codigo)}
              onChange={() => alternarRol(rol.codigo)}
            />
            {rol.nombre}
          </label>
        ))}
      </fieldset>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      <div className="acciones">
        <button type="submit" disabled={enviando}>
          {enviando ? 'Guardando…' : 'Guardar'}
        </button>
        <button type="button" onClick={alCancelar}>
          Cancelar
        </button>
      </div>
    </form>
  );
}
