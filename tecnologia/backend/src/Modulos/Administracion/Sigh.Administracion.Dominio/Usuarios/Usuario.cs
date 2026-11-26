using System.Text.RegularExpressions;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Dominio.Usuarios;

/// <summary>
/// Raíz de agregado: persona del hospital con acceso al sistema (data-model.md, Usuario).
/// </summary>
public sealed partial class Usuario
{
    public const int LongitudMinimaNombreUsuario = 3;
    public const int LongitudMaximaNombreUsuario = 50;
    public const int LongitudMinimaNombreCompleto = 3;
    public const int LongitudMaximaNombreCompleto = 150;
    public const int MaximoIntentosFallidos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private readonly List<RolAsignado> _rolesAsignados = [];

    public Guid Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string NombreCompleto { get; private set; } = string.Empty;
    public string HashContrasena { get; private set; } = string.Empty;
    public EstadoUsuario Estado { get; private set; }
    public bool CambioContrasenaPendiente { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset? UltimoAcceso { get; private set; }

    public IReadOnlyCollection<RolAsignado> RolesAsignados => _rolesAsignados;
    public IReadOnlyList<CodigoRol> Roles => _rolesAsignados.Select(r => r.Rol).ToList();

    private Usuario() { }                                            // requerido por EF Core

    /// <summary>
    /// Crea una cuenta activa con el cambio de contraseña pendiente (FR-006). Exige al menos un rol
    /// (FR-010).
    /// </summary>
    public static Usuario Crear(string nombreUsuario, string nombreCompleto, ContrasenaPropuesta contrasena,
        IEnumerable<CodigoRol> roles, IHasherContrasenas hasher, IReloj reloj)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = ValidarNombreUsuario(nombreUsuario),
            NombreCompleto = ValidarNombreCompleto(nombreCompleto),
            HashContrasena = hasher.Calcular(contrasena.Valor),
            Estado = EstadoUsuario.Activo,
            CambioContrasenaPendiente = true,
            CreadoEn = reloj.Ahora,
        };
        usuario.ReemplazarRoles(roles);
        return usuario;
    }

    /// <summary>Quita los espacios de los extremos y pasa a minúsculas (casos límite de la spec).</summary>
    public static string NormalizarNombreUsuario(string nombreUsuario) =>
        (nombreUsuario ?? string.Empty).Trim().ToLowerInvariant();

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    /// <summary>
    /// Un inicio de sesión solo es exitoso si la cuenta está activa, no está bloqueada y la contraseña
    /// coincide (data-model.md §3). Al quinto fallo consecutivo, bloquea la cuenta 15 minutos (FR-004).
    /// </summary>
    public ResultadoAutenticacion IntentarAutenticar(string contrasena, IHasherContrasenas hasher, IReloj reloj)
    {
        var ahora = reloj.Ahora;

        if (Estado == EstadoUsuario.Desactivado)
            return ResultadoAutenticacion.Rechazado(MotivoRechazo.Desactivado);

        if (EstaBloqueado(ahora))
            return ResultadoAutenticacion.Rechazado(MotivoRechazo.Bloqueado);

        var verificacion = hasher.Verificar(HashContrasena, contrasena ?? string.Empty);
        if (verificacion == ResultadoVerificacionContrasena.Incorrecta)
        {
            IntentosFallidos++;
            if (IntentosFallidos < MaximoIntentosFallidos)
                return ResultadoAutenticacion.Rechazado(MotivoRechazo.ContrasenaIncorrecta);

            BloqueadoHasta = ahora + DuracionBloqueo;
            IntentosFallidos = 0;
            return ResultadoAutenticacion.Rechazado(MotivoRechazo.ContrasenaIncorrecta, cuentaBloqueadaAhora: true);
        }

        if (verificacion == ResultadoVerificacionContrasena.CorrectaRequiereRecalculo)
            HashContrasena = hasher.Calcular(contrasena!);

        IntentosFallidos = 0;
        BloqueadoHasta = null;
        UltimoAcceso = ahora;
        return ResultadoAutenticacion.Correcto();
    }

    /// <summary>Cambia la contraseña propia verificando la actual (FR-006).</summary>
    public void CambiarContrasena(string actual, ContrasenaPropuesta nueva, IHasherContrasenas hasher)
    {
        if (hasher.Verificar(HashContrasena, actual ?? string.Empty) == ResultadoVerificacionContrasena.Incorrecta)
            throw new ReglaNegocioError("La contraseña actual no es correcta.");

        HashContrasena = hasher.Calcular(nueva.Valor);
        CambioContrasenaPendiente = false;
    }

    private void ReemplazarRoles(IEnumerable<CodigoRol> roles)
    {
        var distintos = (roles ?? []).Distinct().ToList();
        if (distintos.Count == 0)
            throw new ReglaNegocioError("El usuario debe tener al menos un rol.");
        if (distintos.Any(r => !Enum.IsDefined(r)))
            throw new ReglaNegocioError("El rol indicado no existe.");

        _rolesAsignados.RemoveAll(r => !distintos.Contains(r.Rol));
        foreach (var rol in distintos.Where(r => _rolesAsignados.All(a => a.Rol != r)))
            _rolesAsignados.Add(new RolAsignado(Id, rol));
    }

    private static string ValidarNombreUsuario(string nombreUsuario)
    {
        var normalizado = NormalizarNombreUsuario(nombreUsuario);
        if (normalizado.Length < LongitudMinimaNombreUsuario || normalizado.Length > LongitudMaximaNombreUsuario
            || !PatronNombreUsuario().IsMatch(normalizado))
        {
            throw new ReglaNegocioError(
                $"El nombre de usuario debe tener entre {LongitudMinimaNombreUsuario} y {LongitudMaximaNombreUsuario} caracteres: letras sin tilde, números, '.', '_' o '-'.");
        }
        return normalizado;
    }

    private static string ValidarNombreCompleto(string nombreCompleto)
    {
        var limpio = (nombreCompleto ?? string.Empty).Trim();
        if (limpio.Length < LongitudMinimaNombreCompleto || limpio.Length > LongitudMaximaNombreCompleto)
        {
            throw new ReglaNegocioError(
                $"El nombre completo debe tener entre {LongitudMinimaNombreCompleto} y {LongitudMaximaNombreCompleto} caracteres.");
        }
        return limpio;
    }

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex PatronNombreUsuario();
}
