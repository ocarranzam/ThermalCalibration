using Thermal.Domain.Common;

namespace Thermal.Domain.EquipmentTypes;

/// <summary>
/// Límite de temperatura de un tipo de equipo (RN-06, RN-07, D-05): un máximo, o una banda de tolerancia
/// alrededor de la consigna de la sesión. Sin valor, el límite está pendiente y las sesiones se capturan sin
/// evaluarlo (RN-09).
/// </summary>
public readonly record struct TemperatureLimit
{
    /// <summary>Rango de <c>DECIMAL(6,2)</c> en <c>dbo.EquipmentType.MaxTemperatureC</c>.</summary>
    public const decimal MinValueC = -9999.99m;
    public const decimal MaxValueC = 9999.99m;

    /// <summary>Máximo de <c>DECIMAL(4,2)</c> en <c>dbo.EquipmentType.ToleranceK</c>.</summary>
    public const decimal MaxToleranceK = 99.99m;
    public const int MaxDecimals = 2;

    private TemperatureLimit(LimitMode mode, decimal? maxC, decimal? toleranceK)
    {
        Mode = mode;
        MaxC = maxC;
        ToleranceK = toleranceK;
    }

    /// <summary>Máximo pendiente de definir.</summary>
    public static TemperatureLimit Pending { get; } = new(LimitMode.Maximum, null, null);

    public LimitMode Mode { get; }

    public decimal? MaxC { get; }

    public decimal? ToleranceK { get; }

    public bool IsDefined => Mode == LimitMode.Maximum ? MaxC.HasValue : ToleranceK.HasValue;

    /// <summary>Límite máximo (o pendiente con <c>null</c>).</summary>
    /// <exception cref="DomainValidationException">Fuera de rango o con más de 2 decimales.</exception>
    public static TemperatureLimit Create(decimal? maxC) => maxC is not { } value
        ? Pending
        : new TemperatureLimit(LimitMode.Maximum, Validated(value, MinValueC, MaxValueC, nameof(EquipmentType.MaxTemperatureC), "El límite"), null);

    /// <summary>Banda de ± <paramref name="toleranceK"/> alrededor de la consigna (o pendiente con <c>null</c>).</summary>
    /// <exception cref="DomainValidationException">Tolerancia no positiva, fuera de rango o con más de 2 decimales.</exception>
    public static TemperatureLimit Band(decimal? toleranceK)
    {
        if (toleranceK is not { } value)
        {
            return new TemperatureLimit(LimitMode.Band, null, null);
        }

        if (value <= 0)
        {
            throw new DomainValidationException(nameof(EquipmentType.ToleranceK), "La tolerancia debe ser mayor que 0");
        }

        return new TemperatureLimit(LimitMode.Band, null, Validated(value, 0, MaxToleranceK, nameof(EquipmentType.ToleranceK), "La tolerancia"));
    }

    /// <summary>Crea el límite de un modo, rechazando el valor que no le corresponde.</summary>
    public static TemperatureLimit For(LimitMode mode, decimal? maxC, decimal? toleranceK) => mode switch
    {
        LimitMode.Maximum when toleranceK.HasValue => throw new DomainValidationException(
            nameof(EquipmentType.ToleranceK), "La tolerancia solo se usa con el modo Band"),
        LimitMode.Band when maxC.HasValue => throw new DomainValidationException(
            nameof(EquipmentType.MaxTemperatureC), "El límite máximo solo se usa con el modo Maximum"),
        LimitMode.Maximum => Create(maxC),
        LimitMode.Band => Band(toleranceK),
        _ => throw new DomainValidationException(nameof(EquipmentType.LimitMode), "Modo de límite no válido"),
    };

    /// <summary>
    /// Evalúa una lectura con comparación estricta (RN-07): con máximo -5,00, una lectura de -5,00 cumple y -4,99
    /// no; con banda 20,00 ± 2,00, 22,00 y 18,00 cumplen, y 22,01 y 17,99 no. Un límite pendiente nunca se supera
    /// (RN-09).
    /// </summary>
    /// <param name="temperatureC">Lectura en °C.</param>
    /// <param name="setpointC">Consigna de la sesión; obligatoria en modo banda.</param>
    public LimitEvaluation Evaluate(decimal temperatureC, decimal? setpointC = null)
    {
        if (!IsDefined)
        {
            return LimitEvaluation.Within;
        }

        if (Mode == LimitMode.Maximum)
        {
            return temperatureC > MaxC!.Value ? LimitEvaluation.Above : LimitEvaluation.Within;
        }

        var setpoint = setpointC ?? throw new DomainValidationException("SetpointC", "La consigna es obligatoria en modo Band");
        return temperatureC > setpoint + ToleranceK!.Value ? LimitEvaluation.Above
            : temperatureC < setpoint - ToleranceK.Value ? LimitEvaluation.Below
            : LimitEvaluation.Within;
    }

    /// <summary>Fuera de límite por arriba (compatibilidad con el modo máximo).</summary>
    public bool IsExceededBy(decimal temperatureC, decimal? setpointC = null) =>
        Evaluate(temperatureC, setpointC) == LimitEvaluation.Above;

    private static decimal Validated(decimal value, decimal min, decimal max, string property, string label)
    {
        if (value < min || value > max)
        {
            throw new DomainValidationException(property, $"{label} debe estar entre {min} y {max}");
        }

        return decimal.Round(value, MaxDecimals) == value
            ? value
            : throw new DomainValidationException(property, $"{label} admite como máximo {MaxDecimals} decimales");
    }
}
