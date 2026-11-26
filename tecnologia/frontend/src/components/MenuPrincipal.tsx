import { NavLink, useNavigate } from 'react-router';
import { useSesion } from '../auth/ContextoSesion';
import { RUTA_CAMBIAR_CONTRASENA, RUTA_INICIAR_SESION } from '../auth/RutaProtegida';

interface OpcionMenu {
  ruta: string;
  etiqueta: string;
  /** Permiso necesario para ver la opción; sin permiso, la ven todos los usuarios con sesión. */
  permiso?: string;
}

/** Opciones del menú. Cada historia agrega aquí sus pantallas con su permiso. */
const OPCIONES: OpcionMenu[] = [
  { ruta: '/', etiqueta: 'Inicio' },
  { ruta: '/usuarios', etiqueta: 'Usuarios', permiso: 'usuarios.gestionar' },
  { ruta: '/matriz-permisos', etiqueta: 'Matriz de permisos', permiso: 'usuarios.gestionar' },
];

export function MenuPrincipal() {
  const { sesion, tienePermiso, cerrarSesion } = useSesion();
  const navegar = useNavigate();

  if (!sesion) return null;

  const salir = async () => {
    await cerrarSesion();
    navegar(RUTA_INICIAR_SESION, { replace: true });
  };

  return (
    <header className="barra">
      <strong className="marca">SIGH</strong>
      <nav aria-label="Menú principal">
        {OPCIONES.filter((o) => !o.permiso || tienePermiso(o.permiso)).map((o) => (
          <NavLink key={o.ruta} to={o.ruta} end>
            {o.etiqueta}
          </NavLink>
        ))}
        <NavLink to={RUTA_CAMBIAR_CONTRASENA}>Cambiar contraseña</NavLink>
      </nav>
      <span className="usuario">{sesion.usuario.nombreCompleto}</span>
      <button type="button" onClick={salir}>
        Cerrar sesión
      </button>
    </header>
  );
}
