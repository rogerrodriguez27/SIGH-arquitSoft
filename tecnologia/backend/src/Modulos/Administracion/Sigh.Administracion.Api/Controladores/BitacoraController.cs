using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigh.Administracion.Api.Respuestas;
using Sigh.Administracion.Aplicacion.Bitacora.ConsultarBitacora;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Permisos;

namespace Sigh.Administracion.Api.Controladores;

/// <summary>
/// Consulta de la bitácora de accesos a la HCE (HU16, RF24). Solo lectura: la bitácora no se modifica
/// ni se elimina por ninguna vía (FR-021).
/// </summary>
[ApiController]
[Route("api/v1/bitacora-accesos")]
[Authorize(Policy = CodigoPermiso.ConsultarBitacora)]
public sealed class BitacoraController(ConsultarBitacoraCasoUso consultarBitacora) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaRespuesta<EntradaBitacoraDto>>> Consultar(
        [FromQuery, Required(ErrorMessage = "La fecha inicial es obligatoria.")] DateTimeOffset? desde,
        [FromQuery, Required(ErrorMessage = "La fecha final es obligatoria.")] DateTimeOffset? hasta,
        [FromQuery] Guid? usuarioId,
        [FromQuery] Guid? historiaId,
        [FromQuery, MaxLength(40)] string? accion,
        [FromQuery] ResultadoAcceso? resultado,
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamano = 20,
        CancellationToken ct = default)
    {
        var actorId = Guid.Parse(User.FindFirst("sub")!.Value);                // del token JWT
        var resultadoConsulta = await consultarBitacora.EjecutarAsync(new ConsultarBitacoraConsulta(
            actorId, desde!.Value, hasta!.Value, usuarioId, historiaId, accion, resultado, pagina, tamano), ct);
        return Ok(PaginaRespuesta<EntradaBitacoraDto>.Desde(resultadoConsulta));
    }
}
