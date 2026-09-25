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
    /// <exception cref="DomainValidationException">Fuera de rango o con más de 2 decimales.</exception>
    public static TemperatureLimit Create(decimal? maxC)
    {
        if (maxC is not { } value)
        {
            return Pending;
        }

        if (value is < MinValueC or > MaxValueC)
        {
            throw Invalid($"El límite debe estar entre {MinValueC} y {MaxValueC} °C");
        }

        if (decimal.Round(value, MaxDecimals) != value)
        {
            throw Invalid($"El límite admite como máximo {MaxDecimals} decimales");
        }

        return new TemperatureLimit(value);
    }

    /// <summary>Fuera de límite solo si la lectura es estrictamente mayor (RN-07). Pendiente: nunca (RN-09).</summary>
    public bool IsExceededBy(decimal temperatureC) => MaxC is { } max && temperatureC > max;

    private static DomainValidationException Invalid(string message) =>
        new(nameof(EquipmentType.MaxTemperatureC), message);
}
