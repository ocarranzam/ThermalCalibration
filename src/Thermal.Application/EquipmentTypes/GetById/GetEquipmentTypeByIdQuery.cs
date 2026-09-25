using Thermal.Application.Abstractions;
using Thermal.Application.Exceptions;

namespace Thermal.Application.EquipmentTypes.GetById;

public sealed record GetEquipmentTypeByIdQuery(int Id) : IQuery<EquipmentTypeDto>;

internal sealed class GetEquipmentTypeByIdQueryHandler(IEquipmentTypeReadStore readStore)
    : IQueryHandler<GetEquipmentTypeByIdQuery, EquipmentTypeDto>
{
    public async Task<EquipmentTypeDto> HandleAsync(
        GetEquipmentTypeByIdQuery query, CancellationToken cancellationToken) =>
        await readStore.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"No existe el tipo de equipo {query.Id}.");
}
