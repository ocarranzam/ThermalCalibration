using Thermal.Application.Abstractions;
using Thermal.Application.Exceptions;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Application.EquipmentTypes.Update;

/// <summary>
/// Reemplaza los datos editables de un tipo de equipo (PUT). <see cref="ExpectedVersion"/> es el
/// ETag de <c>If-Match</c> ya decodificado, o <c>null</c> si el cliente no lo envió.
/// </summary>
public sealed record UpdateEquipmentTypeCommand(
    int Id,
    string Name,
    LimitMode? LimitMode,
    decimal? MaxTemperatureC,
    decimal? ToleranceK,
    int? MinMeasurementPoints,
    int? MinSessionDurationMinutes,
    string? Description,
    bool IsActive,
    byte[]? ExpectedVersion) : ICommand<EquipmentTypeDto>;

internal sealed class UpdateEquipmentTypeCommandHandler(
    IEquipmentTypeRepository repository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateEquipmentTypeCommand, EquipmentTypeDto>
{
    public async Task<EquipmentTypeDto> HandleAsync(
        UpdateEquipmentTypeCommand command, CancellationToken cancellationToken)
    {
        var equipmentType = await repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"No existe el tipo de equipo {command.Id}.");

        if (command.ExpectedVersion is { } expected)
        {
            repository.EnsureVersion(equipmentType, expected);
        }

        equipmentType.Rename(command.Name);
        equipmentType.ChangeLimit(TemperatureLimit.For(
            command.LimitMode ?? LimitMode.Maximum, command.MaxTemperatureC, command.ToleranceK));
        equipmentType.ChangeMinMeasurementPoints(command.MinMeasurementPoints ?? MeasurementPoints.DefaultMinimum);
        equipmentType.ChangeMinSessionDuration(
            command.MinSessionDurationMinutes ?? EquipmentType.BaseSessionDurationMinutes);
        equipmentType.Describe(command.Description);

        if (command.IsActive)
        {
            equipmentType.Activate();
        }
        else
        {
            equipmentType.Deactivate();
        }

        // Después de las validaciones del dominio, para no consultar la base con un nombre inválido.
        if (await repository.NameExistsAsync(equipmentType.Name, command.Id, cancellationToken))
        {
            throw new ConflictException($"Ya existe el tipo de equipo {equipmentType.Name}");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EquipmentTypeDto.From(equipmentType);
    }
}
