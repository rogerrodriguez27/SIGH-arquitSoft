using Sigh.Administracion.Dominio.Bitacora;

namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>Puerto de solo inserción de la bitácora de accesos a la HCE (FR-021, research D5).</summary>
public interface IRepositorioBitacora
{
    /// <summary>Inserta dentro de la transacción compartida activa (FR-022, research D3).</summary>
    Task AgregarAsync(EntradaBitacora entrada, CancellationToken ct = default);

    /// <summary>Inserta con conexión y transacción propias, para que quede aunque la operación se revierta (FR-020).</summary>
    Task AgregarEnTransaccionPropiaAsync(EntradaBitacora entrada, CancellationToken ct = default);
}
