import { useSesion } from '../auth/ContextoSesion';

/** Pantalla principal después de iniciar sesión (SC-004). */
export function Inicio() {
  const { sesion } = useSesion();
  if (!sesion) return null;

  return (
    <main className="contenido">
      <h1>Bienvenido(a), {sesion.usuario.nombreCompleto}</h1>
      <p>Roles: {sesion.usuario.roles.join(', ')}</p>
    </main>
  );
}
