using System.ComponentModel.DataAnnotations;
using Thermal.Application.EquipmentTypes;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Api.EquipmentTypes;

// Schemas de docs/api/thermal-v1.yaml. Aquí solo se valida la forma (obligatorios y tipos);
// los rangos, decimales y formatos los valida el dominio, con los mismos mensajes de HU-02.

/// <summary>Schema <c>CreateEquipmentTypeRequest</c>.</summary>
public sealed record CreateEquipmentTypeRequest
{
    [Required]
    public string? Name { get; init; }

    public LimitMode? LimitMode { get; init; }

    public decimal? MinTemperatureC { get; init; }

    public decimal? MaxTemperatureC { get; init; }

    public decimal? ToleranceK { get; init; }

    public int? MinMeasurementPoints { get; init; }

    public int? MinSessionDurationMinutes { get; init; }

    public string? Description { get; init; }
}

/// <summary>Schema <c>UpdateEquipmentTypeRequest</c> (semántica de reemplazo de PUT).</summary>
public sealed record UpdateEquipmentTypeRequest
{
    [Required]
    public string? Name { get; init; }

    public LimitMode? LimitMode { get; init; }

    public decimal? MinTemperatureC { get; init; }

    public decimal? MaxTemperatureC { get; init; }

    public decimal? ToleranceK { get; init; }

    public int? MinMeasurementPoints { get; init; }

    public int? MinSessionDurationMinutes { get; init; }

    public string? Description { get; init; }

    [Required]
    public bool? IsActive { get; init; }
}

/// <summary>Schemas <c>EquipmentTypeResponse</c> y <c>CreateEquipmentTypeResponse</c>.</summary>
public sealed record EquipmentTypeResponse(
    int Id,
    string Name,
    LimitMode LimitMode,
    decimal? MinTemperatureC,
    decimal? MaxTemperatureC,
    decimal? ToleranceK,
    bool IsLimitDefined,
    bool IsLimitSuggested,
    int MinMeasurementPoints,
    int MinSessionDurationMinutes,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static EquipmentTypeResponse From(EquipmentTypeDto dto) => new(
        dto.Id,
        dto.Name,
        dto.LimitMode,
        dto.MinTemperatureC,
        dto.MaxTemperatureC,
        dto.ToleranceK,
        dto.IsLimitDefined,
        dto.IsLimitSuggested,
        dto.MinMeasurementPoints,
        dto.MinSessionDurationMinutes,
        dto.Description,
        dto.IsActive,
        dto.CreatedAt,
        dto.UpdatedAt);
}
