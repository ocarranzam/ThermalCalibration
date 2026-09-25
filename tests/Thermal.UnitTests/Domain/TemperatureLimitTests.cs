using Thermal.Domain.Common;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.UnitTests.Domain;

/// <summary>Value object <see cref="TemperatureLimit"/>: formato del límite (HU-02) y evaluación estricta (HU-08, HU-05).</summary>
public sealed class TemperatureLimitTests
{
    private static readonly TemperatureLimit FreezerLimit = TemperatureLimit.Create(-5.00m);

    public static TheoryData<decimal, bool> StrictLimitExamples => new()
    {
        { -5.10m, false },
        { -5.00m, false },
        { -4.99m, true },
        { -4.90m, true },
        { 2.00m, true },
    };

    [Theory] // HU-08 · Scenario Outline: Evaluación estricta del límite máximo (Examples con límite -5,00 °C)
    [Trait("Story", "HU-08")]
    [MemberData(nameof(StrictLimitExamples))]
    public void IsExceededBy_UsesStrictlyGreaterThan(decimal readingC, bool expectedAboveLimit) =>
        FreezerLimit.IsExceededBy(readingC).Should().Be(expectedAboveLimit);

    [Fact] // HU-05 · Scenario: Las lecturas no se marcan fuera de límite
    [Trait("Story", "HU-05")]
    public void IsExceededBy_WithPendingLimit_IsNeverTrue() =>
        TemperatureLimit.Pending.IsExceededBy(25.00m).Should().BeFalse();

    [Theory] // HU-02 · Scenario: Rechazar un límite con formato inválido (y 1 o 2 decimales válidos)
    [Trait("Story", "HU-02")]
    [InlineData("-5", true)]
    [InlineData("-5.1", true)]
    [InlineData("-5.12", true)]
    [InlineData("-5.123", false)]
    public void Create_AcceptsAtMostTwoDecimals(string value, bool isValid)
    {
        var create = () => TemperatureLimit.Create(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        if (isValid)
        {
            create.Should().NotThrow();
        }
        else
        {
            create.Should().Throw<DomainValidationException>()
                .WithMessage("El límite admite como máximo 2 decimales");
        }
    }

    [Theory] // HU-02 · Regla: límite dentro del rango de DECIMAL(6,2)
    [Trait("Story", "HU-02")]
    [InlineData(-9999.99, true)]
    [InlineData(9999.99, true)]
    [InlineData(-10000, false)]
    [InlineData(10000, false)]
    public void Create_RespectsDecimal62Range(double value, bool isValid)
    {
        var create = () => TemperatureLimit.Create((decimal)value);

        if (isValid)
        {
            create.Should().NotThrow();
        }
        else
        {
            create.Should().Throw<DomainValidationException>()
                .Which.Property.Should().Be(nameof(EquipmentType.MaxTemperatureC));
        }
    }

    [Fact] // HU-02 · Scenario: Registrar un tipo de equipo con límite pendiente
    [Trait("Story", "HU-02")]
    public void Create_WithNull_ReturnsPending() =>
        TemperatureLimit.Create(null).Should().Be(TemperatureLimit.Pending);
}
