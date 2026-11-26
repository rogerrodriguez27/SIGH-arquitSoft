namespace Sigh.Compartido.Autorizacion;

/// <summary>
/// Marca los endpoints que se pueden usar aunque el usuario tenga un cambio de contraseña pendiente
/// (cambiar la contraseña y cerrar sesión, FR-006). Es un atributo simple, sin dependencias de ASP.NET Core.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class PermitirConCambioContrasenaPendienteAttribute : Attribute;
