using Microsoft.EntityFrameworkCore;
using Thermal.Domain.Companies;
using Thermal.Domain.Equipments;
using Thermal.Domain.EquipmentTypes;
using Thermal.Infrastructure.Persistence.Settings;

namespace Thermal.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core sobre la base <c>ThermalCalibration</c>. El esquema lo define
/// <c>docs/db/01-schema.sql</c> (base primero): no se usan migraciones de EF.
/// </summary>
public sealed class ThermalDbContext(DbContextOptions<ThermalDbContext> options, TimeProvider timeProvider)
    : DbContext(options)
{
    private const string UpdatedAtProperty = "UpdatedAt";

    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Equipment> Equipment => Set<Equipment>();

    internal DbSet<AppSettingRow> AppSettings => Set<AppSettingRow>();

    internal DbSet<ThermocoupleTypeRow> ThermocoupleTypes => Set<ThermocoupleTypeRow>();

    /// <summary>Asigna <c>UpdatedAt</c> a toda entidad editada que tenga esa columna.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = RoundToSecond(timeProvider.GetLocalNow());
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
        {
            if (entry.Metadata.FindProperty(UpdatedAtProperty) is not null)
            {
                entry.Property(UpdatedAtProperty).CurrentValue = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Redondea al segundo más cercano, igual que SQL Server al guardar en <c>DATETIMEOFFSET(0)</c>
    /// (lo hace con <c>CreatedAt</c>). Truncar dejaría <c>UpdatedAt</c> antes que <c>CreatedAt</c>
    /// en una edición hecha en el mismo segundo que la creación.
    /// </summary>
    internal static DateTimeOffset RoundToSecond(DateTimeOffset value)
    {
        var roundedTicks = (value.Ticks + (TimeSpan.TicksPerSecond / 2)) / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond;
        return new DateTimeOffset(roundedTicks, value.Offset);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThermalDbContext).Assembly);
}
