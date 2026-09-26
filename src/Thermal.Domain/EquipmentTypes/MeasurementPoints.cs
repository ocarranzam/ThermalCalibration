namespace Thermal.Domain.EquipmentTypes;

/// <summary>
/// Límites de puntos de medición (D-06). El alcance son equipos de uso individual, de pequeña y mediana escala
/// y de hospitales y clínicas, de hasta 2000 L.
/// </summary>
public static class MeasurementPoints
{
    /// <summary>
    /// Máximo de canales por sesión: 27, lo que exige DIN 12880 para incubadoras y estufas de más de 50 L,
    /// la norma más exigente dentro del alcance.
    /// </summary>
    public const int MaxChannels = 27;

    /// <summary>9 puntos (8 esquinas y el centro): IEC 60068-3-5, DKD-R 5-7 y USP &lt;1079.4&gt; hasta 2000 L.</summary>
    public const int DefaultMinimum = 9;
}
