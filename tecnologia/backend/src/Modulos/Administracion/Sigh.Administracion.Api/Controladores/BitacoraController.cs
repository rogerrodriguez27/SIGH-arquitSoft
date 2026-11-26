using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
[Tags("Bitacora")]
[Authorize(Policy = CodigoPermiso.ConsultarBitacora)]
public sealed class BitacoraController(ConsultarBitacoraCasoUso consultarBitacora) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Consulta la bitácora de accesos a la HCE (FR-024, FR-025) – permiso bitacora.consultar")]
    [EndpointDescription("Rango obligatorio de un año o menos. Resultados de la entrada más reciente a la más antigua. Cada consulta se registra como evento de seguridad BITACORA_CONSULTADA. Nunca incluye contenido clínico.")]
    [ProducesResponseType<PaginaRespuesta<EntradaBitacoraDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
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
