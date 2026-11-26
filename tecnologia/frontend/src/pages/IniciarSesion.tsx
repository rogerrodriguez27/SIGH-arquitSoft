import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { ErrorApi } from '../api/cliente';
import { useSesion } from '../auth/ContextoSesion';
import { RUTA_CAMBIAR_CONTRASENA } from '../auth/RutaProtegida';

const MENSAJE_GENERICO = 'Usuario o contraseña incorrectos';

/** Inicio de sesión (RF21). El mensaje de error es siempre genérico (FR-003). */
export function IniciarSesion() {
  const { sesion, iniciarSesion } = useSesion();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const [nombreUsuario, setNombreUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const desde = (ubicacion.state as { desde?: string } | null)?.desde ?? '/';

  if (sesion) {
    return <Navigate to={sesion.usuario.cambioContrasenaPendiente ? RUTA_CAMBIAR_CONTRASENA : desde} replace />;
  }

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      const nueva = await iniciarSesion(nombreUsuario, contrasena);
      navegar(nueva.usuario.cambioContrasenaPendiente ? RUTA_CAMBIAR_CONTRASENA : desde, { replace: true });
    } catch (e) {
      setError(e instanceof ErrorApi && e.estado === 401 ? MENSAJE_GENERICO : 'No se pudo conectar con el sistema.');
      setContrasena('');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <main className="tarjeta">
      <h1>Iniciar sesión</h1>
      <p>Sistema Integral de Gestión Hospitalaria</p>
      <form onSubmit={enviar}>
        <label>
          Usuario
          <input
            name="nombreUsuario"
            autoComplete="username"
            maxLength={50}
            required
            value={nombreUsuario}
            onChange={(e) => setNombreUsuario(e.target.value)}
          />
        </label>
        <label>
          Contraseña
          <input
            name="contrasena"
            type="password"
            autoComplete="current-password"
            maxLength={128}
            required
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
          />
        </label>
        {error && (
          <p role="alert" className="error">
            {error}
          </p>
        )}
        <button type="submit" disabled={enviando}>
          {enviando ? 'Ingresando…' : 'Ingresar'}
        </button>
      </form>
    </main>
  );
}
