using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thermal.Application.Exceptions;
using Thermal.Application.Settings;
using Thermal.Domain.Settings;

namespace Thermal.Infrastructure.Persistence.Settings;

/// <summary>Fila de <c>dbo.AppSetting</c> (clave-valor). Solo la usa la infraestructura.</summary>
internal sealed class AppSettingRow
{
    public string SettingKey { get; set; } = string.Empty;

    public string SettingValue { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int? UpdatedById { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

internal sealed class AppSettingRowConfiguration : IEntityTypeConfiguration<AppSettingRow>
{
    public void Configure(EntityTypeBuilder<AppSettingRow> builder)
    {
        builder.ToTable("AppSetting", "dbo");
        builder.HasKey(r => r.SettingKey);
        builder.Property(r => r.SettingKey).HasMaxLength(50).IsUnicode(false);
        builder.Property(r => r.SettingValue).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(300);
        builder.Property(r => r.UpdatedAt).HasPrecision(0);
        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

/// <summary>Claves de <c>AppSetting</c> y conversión entre las filas y <see cref="SystemSettings"/>.</summary>
internal static class SettingKeys
{
    public const string SamplingIntervalSeconds = nameof(SamplingIntervalSeconds);
    public const string BaseSessionMinutes = nameof(BaseSessionMinutes);
    public const string MaxSessionMinutes = nameof(MaxSessionMinutes);
    public const string RestPeriodMinutes = nameof(RestPeriodMinutes);
    public const string SensorLossThresholdPct = nameof(SensorLossThresholdPct);
    public const string SensorLossCriticalAfterSamples = nameof(SensorLossCriticalAfterSamples);
    public const string SensorLossFailMinutes = nameof(SensorLossFailMinutes);
    public const string AboveLimitCriticalMinutes = nameof(AboveLimitCriticalMinutes);

    public static SystemSettings ToSettings(IReadOnlyDictionary<string, AppSettingRow> rows)
    {
        int Int(string key) => int.Parse(Value(rows, key), CultureInfo.InvariantCulture);
        return SystemSettings.Create(
            Int(SamplingIntervalSeconds),
            Int(BaseSessionMinutes),
            Int(MaxSessionMinutes),
            Int(RestPeriodMinutes),
            decimal.Parse(Value(rows, SensorLossThresholdPct), CultureInfo.InvariantCulture),
            Int(SensorLossCriticalAfterSamples),
            Int(SensorLossFailMinutes),
            Int(AboveLimitCriticalMinutes));
    }

    public static IEnumerable<(string Key, string Value)> ToValues(SystemSettings settings) =>
    [
        (SamplingIntervalSeconds, Format(settings.SamplingIntervalSeconds)),
        (BaseSessionMinutes, Format(settings.BaseSessionMinutes)),
        (MaxSessionMinutes, Format(settings.MaxSessionMinutes)),
        (RestPeriodMinutes, Format(settings.RestPeriodMinutes)),
        (SensorLossThresholdPct, settings.SensorLossThresholdPct.ToString("0.##", CultureInfo.InvariantCulture)),
        (SensorLossCriticalAfterSamples, Format(settings.SensorLossCriticalAfterSamples)),
        (SensorLossFailMinutes, Format(settings.SensorLossFailMinutes)),
        (AboveLimitCriticalMinutes, Format(settings.AboveLimitCriticalMinutes)),
    ];

    /// <summary>
    /// Versión del conjunto de parámetros (ETag): huella de las <c>RowVersion</c> de las filas, ordenadas por clave.
    /// Cambia si cambia cualquier parámetro.
    /// </summary>
    public static byte[] Version(IEnumerable<AppSettingRow> rows) =>
        SHA256.HashData([.. rows.OrderBy(r => r.SettingKey, StringComparer.Ordinal).SelectMany(r => r.RowVersion)])[..16];

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Value(IReadOnlyDictionary<string, AppSettingRow> rows, string key) =>
        rows.TryGetValue(key, out var row)
            ? row.SettingValue
            : throw new InvalidOperationException($"Falta el parámetro '{key}' en dbo.AppSetting.");
}

internal sealed class SystemSettingsRepository(ThermalDbContext context) : ISystemSettingsRepository
{
    private Dictionary<string, AppSettingRow>? _rows;

    private Dictionary<string, AppSettingRow> Rows =>
        _rows ?? throw new InvalidOperationException("Llamar primero a LoadAsync.");

    public async Task<SystemSettings> LoadAsync(CancellationToken cancellationToken)
    {
        _rows = await context.AppSettings.ToDictionaryAsync(r => r.SettingKey, StringComparer.Ordinal, cancellationToken);
        return SettingKeys.ToSettings(_rows);
    }

    public void EnsureVersion(byte[] expectedVersion)
    {
        if (!expectedVersion.AsSpan().SequenceEqual(SettingKeys.Version(Rows.Values)))
        {
            throw new ConcurrencyConflictException(
                "Los parámetros fueron modificados por otro usuario. Vuelva a leerlos antes de guardar.");
        }
    }

    /// <summary>Solo cambia las filas cuyo valor es distinto; cada una comprueba su <c>RowVersion</c> al guardar.</summary>
    public void Replace(SystemSettings settings)
    {
        foreach (var (key, value) in SettingKeys.ToValues(settings))
        {
            if (Rows[key].SettingValue != value)
            {
                Rows[key].SettingValue = value;
            }
        }
    }
}

internal sealed class SystemSettingsReadStore(ThermalDbContext context) : ISystemSettingsReadStore
{
    public async Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var rows = await context.AppSettings.AsNoTracking()
            .ToDictionaryAsync(r => r.SettingKey, StringComparer.Ordinal, cancellationToken);
        var settings = SettingKeys.ToSettings(rows);
        return new SystemSettingsDto(
            settings.SamplingIntervalSeconds,
            settings.BaseSessionMinutes,
            settings.MaxSessionMinutes,
            settings.RestPeriodMinutes,
            settings.SensorLossThresholdPct,
            settings.SensorLossCriticalAfterSamples,
            settings.SensorLossFailMinutes,
            settings.AboveLimitCriticalMinutes,
            settings.MinValidSamples,
            rows.Values.Max(r => r.UpdatedAt),
            Convert.ToBase64String(SettingKeys.Version(rows.Values)));
    }
}
