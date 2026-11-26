using System.Text;

namespace Sigh.Administracion.Infraestructura.Seguridad;

/// <summary>
/// Configuración del JWT (research D4). Emisor, audiencia y duración vienen de appsettings.json; la
/// clave de firma solo de la variable de entorno Jwt__ClaveFirma (principio VI, ADR-012).
/// </summary>
public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";
    public const int BytesMinimosClave = 32;

    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
    public string ClaveFirma { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; } = 30;

    public byte[] ClaveEnBytes() => Encoding.UTF8.GetBytes(ClaveFirma);

    public bool EsValida() =>
        !string.IsNullOrWhiteSpace(Emisor)
        && !string.IsNullOrWhiteSpace(Audiencia)
        && ClaveEnBytes().Length >= BytesMinimosClave
        && DuracionMinutos > 0;
}
