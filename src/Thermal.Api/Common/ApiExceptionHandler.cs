using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Thermal.Application.Exceptions;
using Thermal.Domain.Common;

namespace Thermal.Api.Common;

/// <summary>
/// Traduce las excepciones del dominio y de la aplicación a Problem Details (RFC 7807),
/// con los códigos de <c>docs/api/thermal-v1.yaml</c>. <c>type</c> y <c>traceId</c> los completa ASP.NET Core.
/// </summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            DomainValidationException validation => new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [JsonNamingPolicy.CamelCase.ConvertName(validation.Property)] = [validation.Message],
                })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Uno o más datos no son válidos",
                Detail = "Revise los campos indicados en errors.",
            },
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Recurso no encontrado", exception),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflicto con el estado del recurso", exception),
            ConcurrencyConflictException => Problem(StatusCodes.Status412PreconditionFailed, "El recurso cambió", exception),
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        problem.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, Exception exception) => new()
    {
        Status = status,
        Title = title,
        Detail = exception.Message,
    };
}
