using Thermal.Infrastructure.Persistence;

namespace Thermal.UnitTests.Infrastructure;

/// <summary>Redondeo de fechas igual al de SQL Server en <c>DATETIMEOFFSET(0)</c> (HU-02, UpdatedAt).</summary>
[Trait("Story", "HU-02")]
public sealed class ThermalDbContextTests
{
    private static readonly TimeSpan Lima = TimeSpan.FromHours(-5);

    [Theory] // HU-02 · Regla: UpdatedAt se guarda redondeado al segundo, como CreatedAt
    [InlineData(499, 28)]
    [InlineData(500, 29)]
    [InlineData(999, 29)]
    public void RoundToSecond_RoundsHalfUpLikeSqlServer(int milliseconds, int expectedSecond)
    {
        var value = new DateTimeOffset(2026, 9, 25, 21, 34, 28, milliseconds, Lima);

        var rounded = ThermalDbContext.RoundToSecond(value);

        rounded.Should().Be(new DateTimeOffset(2026, 9, 25, 21, 34, expectedSecond, Lima));
    }
}
