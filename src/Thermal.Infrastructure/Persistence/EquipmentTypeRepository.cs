using Microsoft.EntityFrameworkCore;
using Thermal.Application.EquipmentTypes;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Infrastructure.Persistence;

internal sealed class EquipmentTypeRepository(ThermalDbContext context) : IEquipmentTypeRepository
{
    public Task<EquipmentType?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.EquipmentTypes.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken) =>
        context.EquipmentTypes.AnyAsync(
            e => e.Name == name && (excludingId == null || e.Id != excludingId), cancellationToken);

    public void Add(EquipmentType equipmentType) => context.EquipmentTypes.Add(equipmentType);

    public void ExpectVersion(EquipmentType equipmentType, byte[] expectedRowVersion) =>
        context.Entry(equipmentType).Property(e => e.RowVersion).OriginalValue = expectedRowVersion;
}

internal sealed class EquipmentTypeReadStore(ThermalDbContext context) : IEquipmentTypeReadStore
{
    public async Task<EquipmentTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await context.EquipmentTypes
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.MaxTemperatureC,
                e.MinSessionDurationMinutes,
                e.Description,
                e.IsActive,
                e.CreatedAt,
                e.UpdatedAt,
                e.RowVersion,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new EquipmentTypeDto(
                row.Id,
                row.Name,
                row.MaxTemperatureC,
                row.MaxTemperatureC.HasValue,
                row.MinSessionDurationMinutes,
                row.Description,
                row.IsActive,
                row.CreatedAt,
                row.UpdatedAt,
                Convert.ToBase64String(row.RowVersion));
    }
}
