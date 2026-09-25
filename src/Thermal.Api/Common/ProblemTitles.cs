using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Thermal.Api.Common;

/// <summary>
/// Títulos en español para los Problem Details que genera ASP.NET Core (validación de forma,
/// 401, 403, 415...), iguales a los de <c>docs/api/thermal-v1.yaml</c>. Los títulos que ya
/// asignó <see cref="ApiExceptionHandler"/> no se tocan.
/// </summary>
internal static class ProblemTitles
{
    private static readonly Dictionary<int, string> Titles = new()
    {
        [StatusCodes.Status400BadRequest] = "Uno o más datos no son válidos",
        [StatusCodes.Status401Unauthorized] = "No autenticado",
        [StatusCodes.Status403Forbidden] = "Acción no permitida",
        [StatusCodes.Status404NotFound] = "Recurso no encontrado",
        [StatusCodes.Status405MethodNotAllowed] = "Método no permitido",
        [StatusCodes.Status409Conflict] = "Conflicto con el estado del recurso",
        [StatusCodes.Status412PreconditionFailed] = "El recurso cambió",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de contenido no admitido",
        [StatusCodes.Status500InternalServerError] = "Error interno del servidor",
    };

    private const string DefaultValidationTitle = "One or more validation errors occurred.";

    public static void Translate(ProblemDetails problem)
    {
        if (problem.Status is not { } status || !Titles.TryGetValue(status, out var title))
        {
            return;
        }

        var isDefaultTitle = problem.Title is null
            || problem.Title == DefaultValidationTitle
            || problem.Title == ReasonPhrases.GetReasonPhrase(status);

        if (isDefaultTitle)
        {
            problem.Title = title;
        }
    }
}
