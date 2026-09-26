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

    public static TheoryData<decimal, LimitEvaluation> BandExamples => new()
    {
        { 22.01m, LimitEvaluation.Above },
        { 22.00m, LimitEvaluation.Within },
        { 20.00m, LimitEvaluation.Within },
        { 18.00m, LimitEvaluation.Within },
        { 17.99m, LimitEvaluation.Below },
    };

    [Theory] // HU-08 · Regla D-05: banda 20,00 ± 2,00 °C con comparación estricta por arriba y por abajo
    [Trait("Story", "HU-08")]
    [MemberData(nameof(BandExamples))]
    public void Evaluate_Band_IsStrictOnBothSides(decimal readingC, LimitEvaluation expected) =>
        TemperatureLimit.Band(2.00m).Evaluate(readingC, setpointC: 20.00m).Should().Be(expected);

    [Fact] // HU-08 · Regla D-05: en modo banda la consigna es obligatoria
    [Trait("Story", "HU-08")]
    public void Evaluate_BandWithoutSetpoint_IsRejected()
    {
        var evaluate = () => TemperatureLimit.Band(2.00m).Evaluate(21m);

        evaluate.Should().Throw<DomainValidationException>().WithMessage("La consigna es obligatoria en modo Band");
    }

    [Fact] // HU-05 · Regla D-05: una banda sin tolerancia (pendiente) no marca lecturas
    [Trait("Story", "HU-05")]
    public void Evaluate_PendingBand_IsAlwaysWithin() =>
        TemperatureLimit.Band(null).Evaluate(35m, setpointC: 20m).Should().Be(LimitEvaluation.Within);

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
