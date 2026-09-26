using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thermal.Api.Common;
using Thermal.Application.Abstractions;
using Thermal.Application.Settings;

namespace Thermal.Api.Settings;

/// <summary>Parámetros del sistema (D-08). Contrato: <c>docs/api/thermal-v1.yaml</c>, tag <c>Settings</c>.</summary>
[ApiController]
[Route("api/v1/settings")]
[Authorize]
public sealed class SettingsController(
    IQueryHandler<GetSystemSettingsQuery, SystemSettingsDto> getHandler,
    ICommandHandler<UpdateSystemSettingsCommand, SystemSettingsDto> updateHandler) : ControllerBase
{
    /// <summary>operationId <c>getSettings</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var settings = await getHandler.HandleAsync(new GetSystemSettingsQuery(), cancellationToken);

        Response.Headers.ETag = EntityTag.From(settings.Version);
        return SettingsResponse.From(settings);
    }

    /// <summary>operationId <c>updateSettings</c>. Solo afecta a las sesiones que se inicien después (RN-08).</summary>
    [HttpPut]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("application/json")]
    public async Task<ActionResult<SettingsResponse>> Update(
        UpdateSettingsRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var settings = await updateHandler.HandleAsync(
            new UpdateSystemSettingsCommand(
                request.SamplingIntervalSeconds!.Value,
                request.BaseSessionMinutes!.Value,
                request.MaxSessionMinutes!.Value,
                request.RestPeriodMinutes!.Value,
                request.SensorLossThresholdPct!.Value,
                request.SensorLossCriticalAfterSamples!.Value,
                request.SensorLossFailMinutes!.Value,
                request.AboveLimitCriticalMinutes!.Value,
                EntityTag.ParseIfMatch(ifMatch)),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(settings.Version);
        return SettingsResponse.From(settings);
    }
}
