using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thermal.Application.ThermocoupleTypes;

namespace Thermal.Infrastructure.Persistence;

/// <summary>Fila de <c>dbo.ThermocoupleType</c> (catálogo fijo, solo lectura).</summary>
internal sealed class ThermocoupleTypeRow
{
    public string ThermocoupleTypeCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal MinRangeC { get; set; }

    public decimal MaxRangeC { get; set; }
}

internal sealed class ThermocoupleTypeRowConfiguration : IEntityTypeConfiguration<ThermocoupleTypeRow>
{
    public void Configure(EntityTypeBuilder<ThermocoupleTypeRow> builder)
    {
        builder.ToTable("ThermocoupleType", "dbo");
        builder.HasKey(r => r.ThermocoupleTypeCode);
        builder.Property(r => r.ThermocoupleTypeCode).HasColumnType("char(1)");
        builder.Property(r => r.Name).HasMaxLength(50);
        builder.Property(r => r.MinRangeC).HasPrecision(7, 2);
        builder.Property(r => r.MaxRangeC).HasPrecision(7, 2);
    }
}

internal sealed class ThermocoupleTypeReadStore(ThermalDbContext context) : IThermocoupleTypeReadStore
{
    public async Task<IReadOnlyList<ThermocoupleTypeDto>> ListAsync(CancellationToken cancellationToken) =>
        await context.ThermocoupleTypes.AsNoTracking()
            .OrderBy(r => r.ThermocoupleTypeCode)
            .Select(r => new ThermocoupleTypeDto(r.ThermocoupleTypeCode, r.Name, r.MinRangeC, r.MaxRangeC))
            .ToListAsync(cancellationToken);
}
