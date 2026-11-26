import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { ErrorApi } from '../api/cliente';
import { useSesion } from '../auth/ContextoSesion';

/** Cambio de la contraseña propia; obligatorio tras una contraseña asignada (FR-005, FR-006). */
export function CambiarContrasena() {
  const { sesion, cambiarContrasena } = useSesion();
  const navegar = useNavigate();
  const [actual, setActual] = useState('');
  const [nueva, setNueva] = useState('');
  const [confirmacion, setConfirmacion] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const obligatorio = sesion?.usuario.cambioContrasenaPendiente ?? false;

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setError(null);
    if (nueva !== confirmacion) {
      setError('La confirmación no coincide con la contraseña nueva.');
      return;
    }
    setEnviando(true);
    try {
      await cambiarContrasena(actual, nueva);
      navegar('/', { replace: true });
    } catch (e) {
      setError(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo conectar con el sistema.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <main className="tarjeta">
      <h1>Cambiar contraseña</h1>
      {obligatorio && (
        <p role="status" className="aviso">
          Debe cambiar su contraseña antes de continuar.
        </p>
      )}
      <p>Al menos 8 caracteres, con letras y números.</p>
      <form onSubmit={enviar}>
        <label>
          Contraseña actual
          <input
            type="password"
            autoComplete="current-password"
            required
            value={actual}
            onChange={(e) => setActual(e.target.value)}
          />
        </label>
        <label>
          Contraseña nueva
          <input
            type="password"
            autoComplete="new-password"
            minLength={8}
            maxLength={128}
            required
            value={nueva}
            onChange={(e) => setNueva(e.target.value)}
          />
        </label>
        <label>
          Confirmar contraseña nueva
          <input
            type="password"
            autoComplete="new-password"
            required
            value={confirmacion}
            onChange={(e) => setConfirmacion(e.target.value)}
          />
        </label>
        {error && (
          <p role="alert" className="error">
            {error}
          </p>
        )}
        <button type="submit" disabled={enviando}>
          {enviando ? 'Guardando…' : 'Cambiar contraseña'}
        </button>
      </form>
    </main>
  );
}
