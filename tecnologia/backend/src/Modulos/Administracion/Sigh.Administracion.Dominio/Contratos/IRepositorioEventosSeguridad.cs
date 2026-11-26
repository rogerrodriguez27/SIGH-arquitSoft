using Sigh.Administracion.Dominio.Seguridad;

namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>
/// Puerto de solo inserción para los eventos de seguridad (research D5). El evento se guarda en una
/// transacción propia, para que quede aunque la operación que lo causó se revierta (research D3).
/// </summary>
public interface IRepositorioEventosSeguridad
{
    Task AgregarAsync(EventoSeguridad evento, CancellationToken ct = default);
}
