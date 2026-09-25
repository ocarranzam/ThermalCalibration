using Thermal.Domain.EquipmentTypes;

namespace Thermal.Application.EquipmentTypes;

/// <summary>
/// Datos de un tipo de equipo que devuelven los casos de uso. <see cref="Version"/> es la
/// <c>RowVersion</c> en Base64, que la API publica como <c>ETag</c>.
/// </summary>
public sealed record EquipmentTypeDto(
    int Id,
    string Name,
    decimal? MaxTemperatureC,
    bool IsLimitDefined,
    int MinSessionDurationMinutes,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Version)
{
    public static EquipmentTypeDto From(EquipmentType equipmentType) => new(
        equipmentType.Id,
        equipmentType.Name,
        equipmentType.MaxTemperatureC,
        equipmentType.MaxTemperature.IsDefined,
        equipmentType.MinSessionDurationMinutes,
        equipmentType.Description,
        equipmentType.IsActive,
        equipmentType.CreatedAt,
        equipmentType.UpdatedAt,
        Convert.ToBase64String(equipmentType.RowVersion));
}
