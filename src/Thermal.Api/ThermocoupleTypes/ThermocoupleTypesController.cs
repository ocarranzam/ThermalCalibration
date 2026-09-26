using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thermal.Application.Abstractions;
using Thermal.Application.ThermocoupleTypes;

namespace Thermal.Api.ThermocoupleTypes;

/// <summary>Schema <c>ThermocoupleTypeResponse</c>.</summary>
public sealed record ThermocoupleTypeResponse(string Code, string Name, decimal MinRangeC, decimal MaxRangeC)
{
    public static ThermocoupleTypeResponse From(ThermocoupleTypeDto dto) => new(dto.Code, dto.Name, dto.MinRangeC, dto.MaxRangeC);
}

/// <summary>Catálogo fijo de tipos de termopar (solo lectura). Contrato: tag <c>Settings</c>.</summary>
[ApiController]
[Route("api/v1/thermocouple-types")]
[Authorize]
public sealed class ThermocoupleTypesController(
    IQueryHandler<ListThermocoupleTypesQuery, IReadOnlyList<ThermocoupleTypeDto>> listHandler) : ControllerBase
{
    /// <summary>operationId <c>listThermocoupleTypes</c>.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ThermocoupleTypeResponse>> List(CancellationToken cancellationToken) =>
        [.. (await listHandler.HandleAsync(new ListThermocoupleTypesQuery(), cancellationToken)).Select(ThermocoupleTypeResponse.From)];
}
