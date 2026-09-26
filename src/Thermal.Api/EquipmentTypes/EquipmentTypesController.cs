using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thermal.Api.Common;
using Thermal.Application.Abstractions;
using Thermal.Application.EquipmentTypes;
using Thermal.Application.EquipmentTypes.Create;
using Thermal.Application.EquipmentTypes.GetById;
using Thermal.Application.EquipmentTypes.Update;

namespace Thermal.Api.EquipmentTypes;

/// <summary>Tipos de equipo (HU-02). Contrato: <c>docs/api/thermal-v1.yaml</c>, tag <c>EquipmentTypes</c>.</summary>
[ApiController]
[Route("api/v1/equipment-types")]
[Authorize]
public sealed class EquipmentTypesController(
    ICommandHandler<CreateEquipmentTypeCommand, EquipmentTypeDto> createHandler,
    IQueryHandler<GetEquipmentTypeByIdQuery, EquipmentTypeDto> getByIdHandler,
    ICommandHandler<UpdateEquipmentTypeCommand, EquipmentTypeDto> updateHandler) : ControllerBase
{
    /// <summary>operationId <c>createEquipmentType</c>.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("application/json")]
    public async Task<ActionResult<EquipmentTypeResponse>> Create(
        CreateEquipmentTypeRequest request, CancellationToken cancellationToken)
    {
        var equipmentType = await createHandler.HandleAsync(
            new CreateEquipmentTypeCommand(
                request.Name!,
                request.LimitMode,
                request.MaxTemperatureC,
                request.ToleranceK,
                request.MinMeasurementPoints,
                request.MinSessionDurationMinutes,
                request.Description),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(equipmentType.Version);
        return CreatedAtAction(
            nameof(GetById), new { id = equipmentType.Id }, EquipmentTypeResponse.From(equipmentType));
    }

    /// <summary>operationId <c>getEquipmentType</c>.</summary>
    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<EquipmentTypeResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var equipmentType = await getByIdHandler.HandleAsync(new GetEquipmentTypeByIdQuery(id), cancellationToken);

        Response.Headers.ETag = EntityTag.From(equipmentType.Version);
        return EquipmentTypeResponse.From(equipmentType);
    }

    /// <summary>operationId <c>updateEquipmentType</c>.</summary>
    [HttpPut("{id:int:min(1)}")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("application/json")]
    public async Task<ActionResult<EquipmentTypeResponse>> Update(
        int id,
        UpdateEquipmentTypeRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var equipmentType = await updateHandler.HandleAsync(
            new UpdateEquipmentTypeCommand(
                id,
                request.Name!,
                request.LimitMode,
                request.MaxTemperatureC,
                request.ToleranceK,
                request.MinMeasurementPoints,
                request.MinSessionDurationMinutes,
                request.Description,
                request.IsActive!.Value,
                EntityTag.ParseIfMatch(ifMatch)),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(equipmentType.Version);
        return EquipmentTypeResponse.From(equipmentType);
    }
}
