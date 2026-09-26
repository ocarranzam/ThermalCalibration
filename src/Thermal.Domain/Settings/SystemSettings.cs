using Thermal.Domain.Common;

namespace Thermal.Domain.Settings;

/// <summary>
/// Parámetros globales del sistema (tabla <c>AppSetting</c>), que mantiene el administrador y que se copian en cada
/// sesión al iniciarla (RN-08). Los rangos son los de la decisión D-08. Es inmutable: una edición crea otra instancia.
/// </summary>
/// <remarks>[PC-01] El intervalo de muestreo es un parámetro más: nada del sistema usa el literal 120 ni 31.</remarks>
public sealed record SystemSettings
{
    public const int MinSamplingIntervalSeconds = 30;
    public const int MaxSamplingIntervalSeconds = 900;
    public const int MinBaseSessionMinutes = 30;
    public const int MaxBaseSessionMinutes = 240;
    public const int MaxPlannableMinutes = 43200;
    public const int MaxRestPeriodMinutes = 240;
    public const int MinCriticalAfterSamples = 2;
    public const int MaxCriticalAfterSamples = 10;
    public const int MinEscalationMinutes = 10;
    public const int MaxEscalationMinutes = 240;

    private SystemSettings(
        int samplingIntervalSeconds,
        int baseSessionMinutes,
        int maxSessionMinutes,
        int restPeriodMinutes,
        decimal sensorLossThresholdPct,
        int sensorLossCriticalAfterSamples,
        int sensorLossFailMinutes,
        int aboveLimitCriticalMinutes)
    {
        SamplingIntervalSeconds = samplingIntervalSeconds;
        BaseSessionMinutes = baseSessionMinutes;
        MaxSessionMinutes = maxSessionMinutes;
        RestPeriodMinutes = restPeriodMinutes;
        SensorLossThresholdPct = sensorLossThresholdPct;
        SensorLossCriticalAfterSamples = sensorLossCriticalAfterSamples;
        SensorLossFailMinutes = sensorLossFailMinutes;
        AboveLimitCriticalMinutes = aboveLimitCriticalMinutes;
    }

    /// <summary>[PC-01] Segundos entre muestras (una lectura por canal).</summary>
    public int SamplingIntervalSeconds { get; }

    /// <summary>Duración base de una sesión y horas de datos válidos exigidas para que quede completa (RN-03, RN-04).</summary>
    public int BaseSessionMinutes { get; }

    /// <summary>Duración máxima que se puede planificar (RN-04).</summary>
    public int MaxSessionMinutes { get; }

    /// <summary>Descanso del kit de medición entre sesiones (RN-17); 0 lo desactiva.</summary>
    public int RestPeriodMinutes { get; }

    /// <summary>Muestra afectada si más de este porcentaje de canales no tiene lectura válida (RN-14).</summary>
    public decimal SensorLossThresholdPct { get; }

    /// <summary>La pérdida de sensores pasa a crítica en esta muestra afectada consecutiva (01 §6.2).</summary>
    public int SensorLossCriticalAfterSamples { get; }

    /// <summary>Minutos consecutivos de muestras afectadas que hacen fallar la sesión (RN-15).</summary>
    public int SensorLossFailMinutes { get; }

    /// <summary>Minutos seguidos fuera de límite que generan una alerta crítica (RN-19).</summary>
    public int AboveLimitCriticalMinutes { get; }

    /// <summary>Muestras válidas mínimas para una sesión completa: 1 h de datos (RN-03), más la muestra en t = 0.</summary>
    public int MinValidSamples => (BaseSessionMinutes * 60 / SamplingIntervalSeconds) + 1;

    /// <exception cref="DomainValidationException">Algún valor fuera de su rango o combinación incoherente.</exception>
    public static SystemSettings Create(
        int samplingIntervalSeconds,
        int baseSessionMinutes,
        int maxSessionMinutes,
        int restPeriodMinutes,
        decimal sensorLossThresholdPct,
        int sensorLossCriticalAfterSamples,
        int sensorLossFailMinutes,
        int aboveLimitCriticalMinutes)
    {
        Ensure(samplingIntervalSeconds is >= MinSamplingIntervalSeconds and <= MaxSamplingIntervalSeconds,
            nameof(SamplingIntervalSeconds), $"El intervalo de muestreo debe estar entre {MinSamplingIntervalSeconds} y {MaxSamplingIntervalSeconds} segundos");
        Ensure(baseSessionMinutes is >= MinBaseSessionMinutes and <= MaxBaseSessionMinutes,
            nameof(BaseSessionMinutes), $"La duración base debe estar entre {MinBaseSessionMinutes} y {MaxBaseSessionMinutes} minutos");
        Ensure(baseSessionMinutes * 60 % samplingIntervalSeconds == 0,
            nameof(SamplingIntervalSeconds), "El intervalo de muestreo debe dividir exactamente la duración base");
        Ensure(maxSessionMinutes >= baseSessionMinutes && maxSessionMinutes <= MaxPlannableMinutes,
            nameof(MaxSessionMinutes), $"La duración máxima debe estar entre la duración base y {MaxPlannableMinutes} minutos");
        Ensure(restPeriodMinutes is >= 0 and <= MaxRestPeriodMinutes,
            nameof(RestPeriodMinutes), $"El descanso debe estar entre 0 y {MaxRestPeriodMinutes} minutos");
        Ensure(sensorLossThresholdPct is > 0 and < 100,
            nameof(SensorLossThresholdPct), "El umbral debe ser mayor que 0 y menor que 100");
        Ensure(decimal.Round(sensorLossThresholdPct, 2) == sensorLossThresholdPct,
            nameof(SensorLossThresholdPct), "El umbral admite como máximo 2 decimales");
        Ensure(sensorLossCriticalAfterSamples is >= MinCriticalAfterSamples and <= MaxCriticalAfterSamples,
            nameof(SensorLossCriticalAfterSamples), $"El escalamiento debe estar entre {MinCriticalAfterSamples} y {MaxCriticalAfterSamples} muestras");
        Ensure(sensorLossFailMinutes is >= MinEscalationMinutes and <= MaxEscalationMinutes,
            nameof(SensorLossFailMinutes), $"Los minutos para declarar la sesión fallida deben estar entre {MinEscalationMinutes} y {MaxEscalationMinutes}");
        Ensure(aboveLimitCriticalMinutes is >= MinEscalationMinutes and <= MaxEscalationMinutes,
            nameof(AboveLimitCriticalMinutes), $"Los minutos fuera de límite para la alerta crítica deben estar entre {MinEscalationMinutes} y {MaxEscalationMinutes}");

        return new SystemSettings(
            samplingIntervalSeconds, baseSessionMinutes, maxSessionMinutes, restPeriodMinutes,
            sensorLossThresholdPct, sensorLossCriticalAfterSamples, sensorLossFailMinutes, aboveLimitCriticalMinutes);
    }

    private static void Ensure(bool condition, string property, string message)
    {
        if (!condition)
        {
            throw new DomainValidationException(property, message);
        }
    }
}
