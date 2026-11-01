using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaAcademico.Application.Common;
using SistemaAcademico.Domain.Common;

namespace SistemaAcademico.Api.Infrastructure;

/// <summary>
/// Traduce excepciones a respuestas RFC 9457 (ProblemDetails). Así los servicios
/// solo lanzan excepciones con significado de negocio y nunca conocen códigos HTTP.
/// Los errores inesperados devuelven 500 sin detalles internos (se registran en el log).
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Datos de entrada inválidos"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autenticado"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autenticado"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning("Petición rechazada ({Status}): {Message}", status, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception switch
            {
                DbUpdateConcurrencyException => "Otro usuario modificó el mismo registro (p. ej. tomó la última vacante). Vuelva a intentarlo.",
                ValidationException => "Revise los campos indicados en 'errors'.",
                _ when status == 500 => "Ocurrió un error inesperado. Intente nuevamente más tarde.",
                _ => exception.Message
            },
            Instance = context.Request.Path
        };

        if (exception is ValidationException ve)
        {
            problem.Extensions["errors"] = ve.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context, ProblemDetails = problem, Exception = exception
        });
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
