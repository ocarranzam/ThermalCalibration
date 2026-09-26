using Thermal.Application.Abstractions;
using Thermal.Application.Companies;
using Thermal.Application.EquipmentTypes;
using Thermal.Application.Exceptions;
using Thermal.Domain.Common;
using Thermal.Domain.Equipments;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Application.Equipments;

public sealed record EquipmentDto(
    int Id,
    int CompanyId,
    int EquipmentTypeId,
    string EquipmentTypeName,
    string Brand,
    string Model,
    bool IsModelConfirmed,
    string SerialNumber,
    string? InternalCode,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Version)
{
    public static EquipmentDto From(Equipment equipment, string equipmentTypeName) => new(
        equipment.Id, equipment.CompanyId, equipment.EquipmentTypeId, equipmentTypeName, equipment.Brand, equipment.Model,
        equipment.IsModelConfirmed, equipment.SerialNumber, equipment.InternalCode, equipment.Notes, equipment.IsActive,
        equipment.CreatedAt, equipment.UpdatedAt, Convert.ToBase64String(equipment.RowVersion));
}

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Indica si la empresa ya tiene otro equipo con esa serie (<c>UQ_Equipment_Company_Serial</c>).</summary>
    Task<bool> SerialExistsAsync(int companyId, string serialNumber, int? excludingId, CancellationToken cancellationToken);

    void Add(Equipment equipment);

    /// <exception cref="ConcurrencyConflictException">La versión ya no es la esperada.</exception>
    void EnsureVersion(Equipment equipment, byte[] expectedRowVersion);
}

public interface IEquipmentReadStore
{
    Task<EquipmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipmentDto>> ListByCompanyAsync(int companyId, CancellationToken cancellationToken);
}

/// <summary>Registra un equipo de una empresa (HU-01). Sin indicación, el modelo se considera confirmado.</summary>
public sealed record CreateEquipmentCommand(
    int CompanyId,
    int EquipmentTypeId,
    string Brand,
    string Model,
    bool? IsModelConfirmed,
    string SerialNumber,
    string? InternalCode,
    string? Notes) : ICommand<EquipmentDto>;

internal sealed class CreateEquipmentCommandHandler(
    ICompanyRepository companies,
    IEquipmentTypeRepository equipmentTypes,
    IEquipmentRepository repository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateEquipmentCommand, EquipmentDto>
{
    public async Task<EquipmentDto> HandleAsync(CreateEquipmentCommand command, CancellationToken cancellationToken)
    {
        _ = await companies.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new NotFoundException($"No existe la empresa {command.CompanyId}.");

        var equipment = new Equipment(
            command.CompanyId, command.EquipmentTypeId, command.Brand, command.Model, command.IsModelConfirmed ?? true,
            command.SerialNumber, command.InternalCode, command.Notes);

        var type = await EquipmentRules.ActiveTypeAsync(equipmentTypes, command.EquipmentTypeId, cancellationToken);
        await EquipmentRules.EnsureUniqueSerialAsync(repository, equipment, excludingId: null, cancellationToken);

        repository.Add(equipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return EquipmentDto.From(equipment, type.Name);
    }
}

/// <summary>Reemplaza los datos editables de un equipo (PUT). La empresa del equipo no cambia.</summary>
public sealed record UpdateEquipmentCommand(
    int Id,
    int EquipmentTypeId,
    string Brand,
    string Model,
    bool IsModelConfirmed,
    string SerialNumber,
    string? InternalCode,
    string? Notes,
    bool IsActive,
    byte[]? ExpectedVersion) : ICommand<EquipmentDto>;

internal sealed class UpdateEquipmentCommandHandler(
    IEquipmentTypeRepository equipmentTypes,
    IEquipmentRepository repository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateEquipmentCommand, EquipmentDto>
{
    public async Task<EquipmentDto> HandleAsync(UpdateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var equipment = await repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"No existe el equipo {command.Id}.");

        if (command.ExpectedVersion is { } expected)
        {
            repository.EnsureVersion(equipment, expected);
        }

        // Un tipo que se desactivó después sigue valiendo para el equipo; solo se exige activo al cambiarlo.
        var type = command.EquipmentTypeId == equipment.EquipmentTypeId
            ? await equipmentTypes.GetByIdAsync(command.EquipmentTypeId, cancellationToken)
                ?? throw new NotFoundException($"No existe el tipo de equipo {command.EquipmentTypeId}.")
            : await EquipmentRules.ActiveTypeAsync(equipmentTypes, command.EquipmentTypeId, cancellationToken);

        equipment.Reclassify(command.EquipmentTypeId);
        equipment.Identify(command.Brand, command.Model, command.IsModelConfirmed);
        equipment.ChangeSerialNumber(command.SerialNumber);
        equipment.Describe(command.InternalCode, command.Notes);
        if (command.IsActive)
        {
            equipment.Activate();
        }
        else
        {
            equipment.Deactivate();
        }

        await EquipmentRules.EnsureUniqueSerialAsync(repository, equipment, command.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return EquipmentDto.From(equipment, type.Name);
    }
}

public sealed record GetEquipmentByIdQuery(int Id) : IQuery<EquipmentDto>;

internal sealed class GetEquipmentByIdQueryHandler(IEquipmentReadStore readStore) : IQueryHandler<GetEquipmentByIdQuery, EquipmentDto>
{
    public async Task<EquipmentDto> HandleAsync(GetEquipmentByIdQuery query, CancellationToken cancellationToken) =>
        await readStore.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"No existe el equipo {query.Id}.");
}

public sealed record ListCompanyEquipmentQuery(int CompanyId) : IQuery<IReadOnlyList<EquipmentDto>>;

internal sealed class ListCompanyEquipmentQueryHandler(ICompanyReadStore companies, IEquipmentReadStore readStore)
    : IQueryHandler<ListCompanyEquipmentQuery, IReadOnlyList<EquipmentDto>>
{
    public async Task<IReadOnlyList<EquipmentDto>> HandleAsync(ListCompanyEquipmentQuery query, CancellationToken cancellationToken)
    {
        _ = await companies.GetByIdAsync(query.CompanyId, cancellationToken)
            ?? throw new NotFoundException($"No existe la empresa {query.CompanyId}.");
        return await readStore.ListByCompanyAsync(query.CompanyId, cancellationToken);
    }
}

/// <summary>Reglas entre agregados de <see cref="Equipment"/> (HU-01 y HU-02).</summary>
internal static class EquipmentRules
{
    /// <summary>El tipo debe existir y estar activo: un tipo desactivado no se ofrece para equipos nuevos (HU-02).</summary>
    public static async Task<EquipmentType> ActiveTypeAsync(
        IEquipmentTypeRepository equipmentTypes, int equipmentTypeId, CancellationToken cancellationToken)
    {
        var type = await equipmentTypes.GetByIdAsync(equipmentTypeId, cancellationToken)
            ?? throw new DomainValidationException(nameof(Equipment.EquipmentTypeId), $"No existe el tipo de equipo {equipmentTypeId}");
        return type.IsActive
            ? type
            : throw new DomainValidationException(nameof(Equipment.EquipmentTypeId), $"El tipo de equipo {type.Name} está desactivado");
    }

    public static async Task EnsureUniqueSerialAsync(
        IEquipmentRepository repository, Equipment equipment, int? excludingId, CancellationToken cancellationToken)
    {
        if (await repository.SerialExistsAsync(equipment.CompanyId, equipment.SerialNumber, excludingId, cancellationToken))
        {
            throw new ConflictException($"La empresa ya tiene un equipo con la serie {equipment.SerialNumber}");
        }
    }
}
