using Thermal.Domain.Common;

namespace Thermal.Domain.EquipmentTypes;

/// <summary>
/// Límite de temperatura de un tipo de equipo (RN-06, RN-07, D-05, D-07): un rango absoluto (mínimo y/o máximo)
/// o una banda de tolerancia alrededor de la consigna de la sesión. Sin valores, el límite está pendiente y las
/// sesiones se capturan sin evaluarlo (RN-09).
/// </summary>
public readonly record struct TemperatureLimit
{
    /// <summary>Rango de <c>DECIMAL(6,2)</c> en <c>MinTemperatureC</c> y <c>MaxTemperatureC</c>.</summary>
    public const decimal MinValueC = -9999.99m;
    public const decimal MaxValueC = 9999.99m;

    /// <summary>Máximo de <c>DECIMAL(4,2)</c> en <c>ToleranceK</c>.</summary>
    public const decimal MaxToleranceK = 99.99m;
    public const int MaxDecimals = 2;

    private TemperatureLimit(LimitMode mode, decimal? minC, decimal? maxC, decimal? toleranceK)
    {
        Mode = mode;
        MinC = minC;
        MaxC = maxC;
        ToleranceK = toleranceK;
    }

    /// <summary>Rango sin valores: límite pendiente de definir.</summary>
    public static TemperatureLimit Pending { get; } = new(LimitMode.Range, null, null, null);

    public LimitMode Mode { get; }

    public decimal? MinC { get; }

    public decimal? MaxC { get; }

    public decimal? ToleranceK { get; }

    public bool IsDefined => Mode == LimitMode.Range ? MinC.HasValue || MaxC.HasValue : ToleranceK.HasValue;

    /// <summary>Rango absoluto; cualquiera de los dos extremos puede faltar (ambos <c>null</c> = pendiente).</summary>
    /// <exception cref="DomainValidationException">Fuera de rango, más de 2 decimales, o mínimo no menor que el máximo.</exception>
    public static TemperatureLimit Range(decimal? minC, decimal? maxC)
    {
        var min = minC is { } lower ? Validated(lower, MinValueC, MaxValueC, nameof(EquipmentType.MinTemperatureC), "El límite mínimo") : (decimal?)null;
        var max = maxC is { } upper ? Validated(upper, MinValueC, MaxValueC, nameof(EquipmentType.MaxTemperatureC), "El límite") : (decimal?)null;

        return min >= max
            ? throw new DomainValidationException(nameof(EquipmentType.MinTemperatureC), "El límite mínimo debe ser menor que el máximo")
            : new TemperatureLimit(LimitMode.Range, min, max, null);
    }

    /// <summary>Banda de ± <paramref name="toleranceK"/> alrededor de la consigna (o pendiente con <c>null</c>).</summary>
    /// <exception cref="DomainValidationException">Tolerancia no positiva, fuera de rango o con más de 2 decimales.</exception>
    public static TemperatureLimit Band(decimal? toleranceK)
    {
        if (toleranceK is not { } value)
        {
            return new TemperatureLimit(LimitMode.Band, null, null, null);
        }

        if (value <= 0)
        {
            throw new DomainValidationException(nameof(EquipmentType.ToleranceK), "La tolerancia debe ser mayor que 0");
        }

        return new TemperatureLimit(LimitMode.Band, null, null, Validated(value, 0, MaxToleranceK, nameof(EquipmentType.ToleranceK), "La tolerancia"));
    }

    /// <summary>Crea el límite de un modo, rechazando los valores que no le corresponden.</summary>
    public static TemperatureLimit For(LimitMode mode, decimal? minC, decimal? maxC, decimal? toleranceK) => mode switch
    {
        LimitMode.Range when toleranceK.HasValue => throw new DomainValidationException(
            nameof(EquipmentType.ToleranceK), "La tolerancia solo se usa con el modo Band"),
        LimitMode.Band when minC.HasValue || maxC.HasValue => throw new DomainValidationException(
            maxC.HasValue ? nameof(EquipmentType.MaxTemperatureC) : nameof(EquipmentType.MinTemperatureC),
            "Los límites mínimo y máximo solo se usan con el modo Range"),
        LimitMode.Range => Range(minC, maxC),
        LimitMode.Band => Band(toleranceK),
        _ => throw new DomainValidationException(nameof(EquipmentType.LimitMode), "Modo de límite no válido"),
    };

    /// <summary>
    /// Evalúa una lectura con comparación estricta (RN-07). Rango +2 … +8 °C: 8,00 y 2,00 cumplen, y 8,01 y 1,99 no.
    /// Solo máximo -5,00: -5,00 cumple y -4,99 no. Banda 20,00 ± 2,00: 22,00 y 18,00 cumplen, y 22,01 y 17,99 no.
    /// Un límite pendiente nunca se supera (RN-09).
    /// </summary>
    /// <param name="temperatureC">Lectura en °C.</param>
    /// <param name="setpointC">Consigna de la sesión; obligatoria en modo banda.</param>
    public LimitEvaluation Evaluate(decimal temperatureC, decimal? setpointC = null)
    {
        if (!IsDefined)
        {
            return LimitEvaluation.Within;
        }

        var (lower, upper) = Mode == LimitMode.Range
            ? (MinC, MaxC)
            : BandBounds(setpointC ?? throw new DomainValidationException("SetpointC", "La consigna es obligatoria en modo Band"));

        return temperatureC > upper ? LimitEvaluation.Above
            : temperatureC < lower ? LimitEvaluation.Below
            : LimitEvaluation.Within;
    }

    /// <summary>Fuera de límite por arriba.</summary>
    public bool IsExceededBy(decimal temperatureC, decimal? setpointC = null) =>
        Evaluate(temperatureC, setpointC) == LimitEvaluation.Above;

    private (decimal? Lower, decimal? Upper) BandBounds(decimal setpointC) =>
        (setpointC - ToleranceK!.Value, setpointC + ToleranceK.Value);

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
