using Thermal.Domain.Common;
using Thermal.Domain.Settings;

namespace Thermal.UnitTests.Domain;

/// <summary>Parámetros del sistema y sus rangos (HU-02, D-08).</summary>
[Trait("Story", "HU-02")]
public sealed class SystemSettingsTests
{
    private static SystemSettings Create(
        int samplingIntervalSeconds = 60,
        int baseSessionMinutes = 60,
        int maxSessionMinutes = 10080,
        int restPeriodMinutes = 15,
        decimal sensorLossThresholdPct = 60m,
        int sensorLossCriticalAfterSamples = 3,
        int sensorLossFailMinutes = 30,
        int aboveLimitCriticalMinutes = 30) =>
        SystemSettings.Create(
            samplingIntervalSeconds, baseSessionMinutes, maxSessionMinutes, restPeriodMinutes, sensorLossThresholdPct,
            sensorLossCriticalAfterSamples, sensorLossFailMinutes, aboveLimitCriticalMinutes);

    [Fact] // HU-02 · Regla: el administrador mantiene los parámetros de AppSetting (valores iniciales)
    public void Create_WithValidValues_KeepsThem()
    {
        var settings = Create(restPeriodMinutes: 0, sensorLossThresholdPct: 62.5m);

        settings.SamplingIntervalSeconds.Should().Be(60);
        settings.BaseSessionMinutes.Should().Be(60);
        settings.MaxSessionMinutes.Should().Be(10080);
        settings.RestPeriodMinutes.Should().Be(0, "0 desactiva el descanso del kit");
        settings.SensorLossThresholdPct.Should().Be(62.5m);
    }

    [Theory] // HU-02 · Regla RN-03 y PC-01: muestras válidas mínimas según el intervalo (1 h de datos + 1)
    [InlineData(60, 60, 61)]
    [InlineData(30, 60, 121)]
    [InlineData(300, 60, 13)]
    [InlineData(900, 240, 17)]
    public void MinValidSamples_DependsOnTheSamplingInterval(int intervalSeconds, int baseMinutes, int expected) =>
        Create(samplingIntervalSeconds: intervalSeconds, baseSessionMinutes: baseMinutes).MinValidSamples.Should().Be(expected);

    [Theory] // HU-02 · Regla D-08: rangos de cada parámetro
    [InlineData(29, 60, 10080, 15, 60, 3, 30, 30, "SamplingIntervalSeconds", "El intervalo de muestreo debe estar entre 30 y 900 segundos")]
    [InlineData(901, 60, 10080, 15, 60, 3, 30, 30, "SamplingIntervalSeconds", "El intervalo de muestreo debe estar entre 30 y 900 segundos")]
    [InlineData(60, 29, 10080, 15, 60, 3, 30, 30, "BaseSessionMinutes", "La duración base debe estar entre 30 y 240 minutos")]
    [InlineData(60, 241, 10080, 15, 60, 3, 30, 30, "BaseSessionMinutes", "La duración base debe estar entre 30 y 240 minutos")]
    [InlineData(70, 60, 10080, 15, 60, 3, 30, 30, "SamplingIntervalSeconds", "El intervalo de muestreo debe dividir exactamente la duración base")]
    [InlineData(60, 60, 59, 15, 60, 3, 30, 30, "MaxSessionMinutes", "La duración máxima debe estar entre la duración base y 43200 minutos")]
    [InlineData(60, 60, 43201, 15, 60, 3, 30, 30, "MaxSessionMinutes", "La duración máxima debe estar entre la duración base y 43200 minutos")]
    [InlineData(60, 60, 10080, -1, 60, 3, 30, 30, "RestPeriodMinutes", "El descanso debe estar entre 0 y 240 minutos")]
    [InlineData(60, 60, 10080, 241, 60, 3, 30, 30, "RestPeriodMinutes", "El descanso debe estar entre 0 y 240 minutos")]
    [InlineData(60, 60, 10080, 15, 0, 3, 30, 30, "SensorLossThresholdPct", "El umbral debe ser mayor que 0 y menor que 100")]
    [InlineData(60, 60, 10080, 15, 100, 3, 30, 30, "SensorLossThresholdPct", "El umbral debe ser mayor que 0 y menor que 100")]
    [InlineData(60, 60, 10080, 15, 60, 1, 30, 30, "SensorLossCriticalAfterSamples", "El escalamiento debe estar entre 2 y 10 muestras")]
    [InlineData(60, 60, 10080, 15, 60, 11, 30, 30, "SensorLossCriticalAfterSamples", "El escalamiento debe estar entre 2 y 10 muestras")]
    [InlineData(60, 60, 10080, 15, 60, 3, 9, 30, "SensorLossFailMinutes", "Los minutos para declarar la sesión fallida deben estar entre 10 y 240")]
    [InlineData(60, 60, 10080, 15, 60, 3, 241, 30, "SensorLossFailMinutes", "Los minutos para declarar la sesión fallida deben estar entre 10 y 240")]
    [InlineData(60, 60, 10080, 15, 60, 3, 30, 9, "AboveLimitCriticalMinutes", "Los minutos fuera de límite para la alerta crítica deben estar entre 10 y 240")]
    [InlineData(60, 60, 10080, 15, 60, 3, 30, 241, "AboveLimitCriticalMinutes", "Los minutos fuera de límite para la alerta crítica deben estar entre 10 y 240")]
    public void Create_OutOfRange_IsRejected(
        int interval, int baseMinutes, int maxMinutes, int rest, int threshold, int criticalAfter, int failMinutes,
        int aboveLimitMinutes, string property, string message)
    {
        var create = () => Create(interval, baseMinutes, maxMinutes, rest, threshold, criticalAfter, failMinutes, aboveLimitMinutes);

        create.Should().Throw<DomainValidationException>().WithMessage(message).Which.Property.Should().Be(property);
    }

    [Fact] // HU-02 · Regla D-08: el umbral admite como máximo 2 decimales
    public void Create_ThresholdWithThreeDecimals_IsRejected()
    {
        var create = () => Create(sensorLossThresholdPct: 60.125m);

        create.Should().Throw<DomainValidationException>().WithMessage("El umbral admite como máximo 2 decimales");
    }

    [Fact] // HU-02 · Regla D-08: la duración máxima puede ser igual a la base
    public void Create_MaxEqualToBase_IsAccepted() =>
        Create(baseSessionMinutes: 240, maxSessionMinutes: 240).MaxSessionMinutes.Should().Be(240);
}
