using Thermal.Application.Exceptions;
using Thermal.Domain.Settings;
using Thermal.Infrastructure.Persistence;
using Thermal.Infrastructure.Persistence.Settings;

namespace Thermal.IntegrationTests;

/// <summary>
/// Parámetros del sistema (<c>dbo.AppSetting</c>, HU-02 y D-08) y catálogo de termopares contra el esquema real.
/// Es la única clase que escribe en <c>AppSetting</c>; sus pruebas se ejecutan en serie.
/// </summary>
[Trait("Story", "HU-02")]
public sealed class SettingsPersistenceTests(SqlServerFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static SystemSettings With(SystemSettings current, int restPeriodMinutes) => SystemSettings.Create(
        current.SamplingIntervalSeconds, current.BaseSessionMinutes, current.MaxSessionMinutes, restPeriodMinutes,
        current.SensorLossThresholdPct, current.SensorLossCriticalAfterSamples, current.SensorLossFailMinutes,
        current.AboveLimitCriticalMinutes);

    [Fact] // HU-02 · Regla: los datos iniciales de AppSetting son válidos según D-08
    public async Task Get_SeedValues_AreReadWithVersion()
    {
        await using var context = database.CreateDbContext();

        var settings = await new SystemSettingsReadStore(context).GetAsync(Token);

        settings.BaseSessionMinutes.Should().Be(60);
        settings.MaxSessionMinutes.Should().Be(10080);
        settings.MinValidSamples.Should().Be((settings.BaseSessionMinutes * 60 / settings.SamplingIntervalSeconds) + 1);
        Convert.FromBase64String(settings.Version).Should().HaveCount(16);
    }

    [Fact] // HU-02 · Regla: editar un parámetro cambia la versión y UpdatedAt; releer da los valores nuevos
    public async Task Replace_ChangedValue_IsPersistedAndChangesVersion()
    {
        await using var context = database.CreateDbContext();
        var before = await new SystemSettingsReadStore(context).GetAsync(Token);
        var repository = new SystemSettingsRepository(context);
        var current = await repository.LoadAsync(Token);
        repository.EnsureVersion(Convert.FromBase64String(before.Version));

        repository.Replace(With(current, current.RestPeriodMinutes == 20 ? 25 : 20));
        await new UnitOfWork(context).SaveChangesAsync(Token);

        await using var reader = database.CreateDbContext();
        var after = await new SystemSettingsReadStore(reader).GetAsync(Token);
        after.RestPeriodMinutes.Should().NotBe(before.RestPeriodMinutes);
        after.Version.Should().NotBe(before.Version);
        after.UpdatedAt.Millisecond.Should().Be(0, "la columna es DATETIMEOFFSET(0)");
    }

    [Fact] // HU-02 · Regla: concurrencia optimista; una versión anterior se rechaza (412)
    public async Task EnsureVersion_StaleVersion_ThrowsConcurrencyConflict()
    {
        await using var context = database.CreateDbContext();
        var stale = Convert.FromBase64String((await new SystemSettingsReadStore(context).GetAsync(Token)).Version);
        var repository = new SystemSettingsRepository(context);
        var current = await repository.LoadAsync(Token);
        repository.Replace(With(current, current.RestPeriodMinutes == 30 ? 35 : 30));
        await new UnitOfWork(context).SaveChangesAsync(Token);

        await using var other = database.CreateDbContext();
        var otherRepository = new SystemSettingsRepository(other);
        await otherRepository.LoadAsync(Token);

        var ensure = () => otherRepository.EnsureVersion(stale);

        ensure.Should().Throw<ConcurrencyConflictException>();
    }

    [Fact] // HU-02 · Regla: catálogo fijo de tipos de termopar (T y K)
    public async Task ThermocoupleTypes_AreListedByCode()
    {
        await using var context = database.CreateDbContext();

        var types = await new ThermocoupleTypeReadStore(context).ListAsync(Token);

        types.Select(t => t.Code).Should().Equal("K", "T");
        types.Single(t => t.Code == "T").MaxRangeC.Should().Be(350.00m);
    }
}
