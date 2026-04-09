using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CalorieTracker.Api.Middleware;

/// <summary>
/// Middleware centralizado de gestión de errores para ASP.NET Core 9.
/// Intercepta todas las excepciones no controladas y devuelve respuestas
/// estructuradas sin exponer detalles internos al cliente.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, type) = exception switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "No autorizado",
                "No tiene permisos para acceder a este recurso.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Conflicto de operación",
                "La operación no pudo completarse debido a un conflicto con el estado actual del recurso.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                "Los datos de la solicitud no son válidos.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        _logger.LogError(exception, "Excepción no controlada [{StatusCode}]: {Message}", statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Detail = detail,
                Type   = type
            },
            cancellationToken);

        return true;
    }
}
