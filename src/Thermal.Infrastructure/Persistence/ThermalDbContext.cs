using Microsoft.EntityFrameworkCore;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core sobre la base <c>ThermalCalibration</c>. El esquema lo define
/// <c>docs/db/01-schema.sql</c> (base primero): no se usan migraciones de EF.
/// </summary>
public sealed class ThermalDbContext(DbContextOptions<ThermalDbContext> options, TimeProvider timeProvider)
    : DbContext(options)
{
    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // DATETIMEOFFSET(0): sin fracciones de segundo, para que la respuesta coincida con lo guardado.
        var now = timeProvider.GetLocalNow();
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
        foreach (var entry in ChangeTracker.Entries<EquipmentType>().Where(e => e.State == EntityState.Modified))
        {
            entry.Property(e => e.UpdatedAt).CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThermalDbContext).Assembly);
}
