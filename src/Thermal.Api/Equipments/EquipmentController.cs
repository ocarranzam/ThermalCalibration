using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thermal.Api.Common;
using Thermal.Application.Abstractions;
using Thermal.Application.Equipments;

namespace Thermal.Api.Equipments;

/// <summary>
/// Equipos bajo prueba (HU-01). Se crean y se listan desde su empresa (<c>/api/v1/companies/{id}/equipment</c>).
/// Contrato: <c>docs/api/thermal-v1.yaml</c>, tag <c>Companies</c>.
/// </summary>
[ApiController]
[Route("api/v1/equipment")]
[Authorize]
public sealed class EquipmentController(
    IQueryHandler<GetEquipmentByIdQuery, EquipmentDto> getByIdHandler,
    ICommandHandler<UpdateEquipmentCommand, EquipmentDto> updateHandler) : ControllerBase
{
    /// <summary>operationId <c>getEquipment</c>.</summary>
    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<EquipmentResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var equipment = await getByIdHandler.HandleAsync(new GetEquipmentByIdQuery(id), cancellationToken);

        Response.Headers.ETag = EntityTag.From(equipment.Version);
        return EquipmentResponse.From(equipment);
    }

    /// <summary>operationId <c>updateEquipment</c>.</summary>
    [HttpPut("{id:int:min(1)}")]
    [Authorize(Roles = Roles.Editors)]
    [Consumes("application/json")]
    public async Task<ActionResult<EquipmentResponse>> Update(
        int id,
        UpdateEquipmentRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var equipment = await updateHandler.HandleAsync(
            new UpdateEquipmentCommand(
                id,
                request.EquipmentTypeId!.Value,
                request.Brand!,
                request.Model!,
                request.IsModelConfirmed!.Value,
                request.SerialNumber!,
                request.InternalCode,
                request.Notes,
                request.IsActive!.Value,
                EntityTag.ParseIfMatch(ifMatch)),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(equipment.Version);
        return EquipmentResponse.From(equipment);
    }
}
