using Thermal.Application.Abstractions;
using Thermal.Application.Exceptions;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Application.EquipmentTypes.Create;

/// <summary>Registra un tipo de equipo activo (HU-02). Sin duración mínima se usa la base de 60 min.</summary>
public sealed record CreateEquipmentTypeCommand(
    string Name,
    decimal? MaxTemperatureC,
    int? MinSessionDurationMinutes,
    string? Description) : ICommand<EquipmentTypeDto>;

internal sealed class CreateEquipmentTypeCommandHandler(
    IEquipmentTypeRepository repository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateEquipmentTypeCommand, EquipmentTypeDto>
{
    public async Task<EquipmentTypeDto> HandleAsync(
        CreateEquipmentTypeCommand command, CancellationToken cancellationToken)
    {
        var equipmentType = new EquipmentType(
            command.Name,
            command.MaxTemperatureC,
            command.MinSessionDurationMinutes ?? EquipmentType.BaseSessionDurationMinutes,
            command.Description);

        if (await repository.NameExistsAsync(equipmentType.Name, excludingId: null, cancellationToken))
        {
            throw new ConflictException($"Ya existe el tipo de equipo {equipmentType.Name}");
        }

        repository.Add(equipmentType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EquipmentTypeDto.From(equipmentType);
    }
}
