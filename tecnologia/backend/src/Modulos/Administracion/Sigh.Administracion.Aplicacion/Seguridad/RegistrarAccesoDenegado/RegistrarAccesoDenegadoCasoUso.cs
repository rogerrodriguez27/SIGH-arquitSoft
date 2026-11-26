using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Seguridad.RegistrarAccesoDenegado;

/// <summary>
/// Registra como ACCESO_DENEGADO cada operación rechazada por falta de permisos (EQ-04, historia 2,
/// escenario 6). El evento se guarda en su propia transacción (research D3).
/// </summary>
public sealed class RegistrarAccesoDenegadoCasoUso(IRepositorioEventosSeguridad eventos, IReloj reloj)
{
    public Task EjecutarAsync(RegistrarAccesoDenegadoComando comando, CancellationToken ct = default)
    {
        var detalle = $"{comando.Metodo} {comando.Ruta}; {comando.Motivo}";
        if (detalle.Length > EventoSeguridad.LongitudMaximaDetalle)
            detalle = detalle[..EventoSeguridad.LongitudMaximaDetalle];

        return eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.AccesoDenegado, reloj,
            comando.ActorId, objetivoId: null, detalle), ct);
    }
}
