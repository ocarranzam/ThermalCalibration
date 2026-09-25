using Thermal.Domain.Common;

namespace Thermal.Domain.EquipmentTypes;

/// <summary>
/// Límite máximo de temperatura de un tipo de equipo (RN-06, RN-07).
/// Sin valor, el límite está pendiente y las sesiones se capturan sin evaluarlo (RN-09).
/// </summary>
public readonly record struct TemperatureLimit
{
    /// <summary>Rango de <c>DECIMAL(6,2)</c> en <c>dbo.EquipmentType.MaxTemperatureC</c>.</summary>
    public const decimal MinValueC = -9999.99m;
    public const decimal MaxValueC = 9999.99m;
    public const int MaxDecimals = 2;

    private TemperatureLimit(decimal? maxC) => MaxC = maxC;

    public static TemperatureLimit Pending { get; } = new(null);

    public decimal? MaxC { get; }

    public bool IsDefined => MaxC.HasValue;

    /// <summary>
    /// Crea el límite validando el rango y los decimales. La base redondearía un tercer decimal
    /// en silencio, así que el rechazo solo lo puede hacer el dominio.
    /// </summary>
    public static TemperatureLimit Create(decimal? maxC)
    {
        if (maxC is not { } value)
        {
            return Pending;
        }

        if (value is < MinValueC or > MaxValueC)
        {
            throw new DomainValidationException(
                "maxTemperatureC", $"El límite debe estar entre {MinValueC} y {MaxValueC} °C");
        }

        if (decimal.Round(value, MaxDecimals) != value)
        {
            throw new DomainValidationException("maxTemperatureC", "El límite admite como máximo 2 decimales");
        }

        return new TemperatureLimit(value);
    }

    /// <summary>Fuera de límite solo si la lectura es estrictamente mayor (RN-07).</summary>
    public bool IsExceededBy(decimal temperatureC) => MaxC is { } max && temperatureC > max;
}
