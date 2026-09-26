using Microsoft.EntityFrameworkCore;
using Thermal.Application.EquipmentTypes;
using Thermal.Application.Exceptions;
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

    public void EnsureVersion(EquipmentType equipmentType, byte[] expectedRowVersion)
    {
        if (!expectedRowVersion.AsSpan().SequenceEqual(equipmentType.RowVersion))
        {
            throw new ConcurrencyConflictException(
                "El tipo de equipo fue modificado por otro usuario. Vuelva a leerlo antes de guardar.");
        }

        // Si otro usuario guarda entre esta lectura y el SaveChanges, el UPDATE no encuentra la fila (412).
        context.Entry(equipmentType).Property(e => e.RowVersion).OriginalValue = expectedRowVersion;
    }
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
                e.LimitMode,
                e.MaxTemperatureC,
                e.ToleranceK,
                e.MinMeasurementPoints,
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
                row.LimitMode,
                row.MaxTemperatureC,
                row.ToleranceK,
                row.LimitMode == LimitMode.Maximum ? row.MaxTemperatureC.HasValue : row.ToleranceK.HasValue,
                row.MinMeasurementPoints,
                row.MinSessionDurationMinutes,
                row.Description,
                row.IsActive,
                row.CreatedAt,
                row.UpdatedAt,
                Convert.ToBase64String(row.RowVersion));
    }
}
