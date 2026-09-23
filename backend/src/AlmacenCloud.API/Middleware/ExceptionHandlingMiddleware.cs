using AlmacenCloud.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenCloud.API.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
                AuthenticationException => (StatusCodes.Status401Unauthorized, "No autorizado"),
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                BusinessRuleException => (StatusCodes.Status409Conflict, "Regla de negocio"),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
                _ => (StatusCodes.Status500InternalServerError, "Error interno")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled request error");

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == 500 ? "Ocurrió un error inesperado." : exception.Message
            });
        }
    }
}
