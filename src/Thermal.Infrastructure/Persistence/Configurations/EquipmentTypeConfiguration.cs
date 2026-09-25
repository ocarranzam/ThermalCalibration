using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="EquipmentType"/> a <c>dbo.EquipmentType</c> (01-schema.sql).</summary>
internal sealed class EquipmentTypeConfiguration : IEntityTypeConfiguration<EquipmentType>
{
    public void Configure(EntityTypeBuilder<EquipmentType> builder)
    {
        builder.ToTable("EquipmentType", "dbo");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("EquipmentTypeId").ValueGeneratedOnAdd();

        builder.Property(e => e.Name).HasMaxLength(EquipmentType.NameMaxLength).IsRequired();
        builder.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UQ_EquipmentType_Name");

        builder.Property(e => e.MaxTemperatureC).HasPrecision(6, 2);
        builder.Property(e => e.MinSessionDurationMinutes).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(EquipmentType.DescriptionMaxLength);
        builder.Property(e => e.IsActive).IsRequired();

        // Con el valor por defecto de CLR, EF no envía la columna y la base aplica DF_EquipmentType_CreatedAt.
        builder.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(e => e.UpdatedAt).HasPrecision(0);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Ignore(e => e.MaxTemperature);
    }
}
