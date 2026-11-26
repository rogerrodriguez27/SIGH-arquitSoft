using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sigh.Compartido.Errores;

namespace Sigh.Api.Middlewares;

/// <summary>
/// Traduce los errores de negocio a respuestas controladas en application/problem+json, sin trazas
/// internas ni información clínica (principio III, docs §6.4).
/// </summary>
public sealed class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var (estado, titulo, detalle) = exception switch
        {
            ReglaNegocioError e => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio", e.Message),
            RecursoNoEncontradoError e => (StatusCodes.Status404NotFound, "Recurso no encontrado", e.Message),
            ConflictoError e => (StatusCodes.Status409Conflict, "Conflicto", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno",
                "Ocurrió un error inesperado. Intente nuevamente."),
        };

        if (estado == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado en {Ruta}", httpContext.Request.Path);

        httpContext.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle },
        });
    }
}
