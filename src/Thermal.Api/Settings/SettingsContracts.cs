using System.ComponentModel.DataAnnotations;
using Thermal.Application.Settings;

namespace Thermal.Api.Settings;

// Schemas de docs/api/thermal-v1.yaml. Aquí solo se valida la forma; los rangos (D-08) los valida el dominio.

/// <summary>Schema <c>UpdateSettingsRequest</c> (semántica de reemplazo de PUT: todos los parámetros).</summary>
public sealed record UpdateSettingsRequest
{
    [Required(ErrorMessage = "El intervalo de muestreo es obligatorio")]
    public int? SamplingIntervalSeconds { get; init; }

    [Required(ErrorMessage = "La duración base es obligatoria")]
    public int? BaseSessionMinutes { get; init; }

    [Required(ErrorMessage = "La duración máxima es obligatoria")]
    public int? MaxSessionMinutes { get; init; }

    [Required(ErrorMessage = "El descanso del kit es obligatorio")]
    public int? RestPeriodMinutes { get; init; }

    [Required(ErrorMessage = "El umbral de pérdida de sensores es obligatorio")]
    public decimal? SensorLossThresholdPct { get; init; }

    [Required(ErrorMessage = "Las muestras para alerta crítica son obligatorias")]
    public int? SensorLossCriticalAfterSamples { get; init; }

    [Required(ErrorMessage = "Los minutos para fallar la sesión son obligatorios")]
    public int? SensorLossFailMinutes { get; init; }

    [Required(ErrorMessage = "Los minutos fuera de límite para alerta crítica son obligatorios")]
    public int? AboveLimitCriticalMinutes { get; init; }
}

/// <summary>Schema <c>SettingsResponse</c>.</summary>
public sealed record SettingsResponse(
    int SamplingIntervalSeconds,
    int BaseSessionMinutes,
    int MaxSessionMinutes,
    int RestPeriodMinutes,
    decimal SensorLossThresholdPct,
    int SensorLossCriticalAfterSamples,
    int SensorLossFailMinutes,
    int AboveLimitCriticalMinutes,
    int MinValidSamples,
    DateTimeOffset UpdatedAt)
{
    public static SettingsResponse From(SystemSettingsDto dto) => new(
        dto.SamplingIntervalSeconds,
        dto.BaseSessionMinutes,
        dto.MaxSessionMinutes,
        dto.RestPeriodMinutes,
        dto.SensorLossThresholdPct,
        dto.SensorLossCriticalAfterSamples,
        dto.SensorLossFailMinutes,
        dto.AboveLimitCriticalMinutes,
        dto.MinValidSamples,
        dto.UpdatedAt);
}
