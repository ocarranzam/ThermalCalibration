using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thermal.Domain.Companies;
using Thermal.Domain.Equipments;

namespace Thermal.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de <see cref="Company"/> a <c>dbo.Company</c> (01-schema.sql).</summary>
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Company", "dbo");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("CompanyId").ValueGeneratedOnAdd();
        builder.Property(c => c.TaxId).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(c => c.TaxId).IsUnique().HasDatabaseName("UQ_Company_TaxId");
        builder.Property(c => c.Name).HasMaxLength(Company.NameMaxLength).IsRequired();
        builder.Property(c => c.ContactName).HasMaxLength(Company.ContactNameMaxLength);
        builder.Property(c => c.Phone).HasMaxLength(Company.PhoneMaxLength).IsUnicode(false);
        builder.Property(c => c.Email).HasMaxLength(Company.EmailMaxLength);
        builder.Property(c => c.Address).HasMaxLength(Company.AddressMaxLength);
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedAt).HasPrecision(0).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(c => c.UpdatedAt).HasPrecision(0);
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

/// <summary>Mapeo de <see cref="Equipment"/> a <c>dbo.Equipment</c> (01-schema.sql).</summary>
internal sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment", "dbo");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("EquipmentId").ValueGeneratedOnAdd();
        builder.Property(e => e.CompanyId).IsRequired();
        builder.Property(e => e.EquipmentTypeId).IsRequired();
        builder.Property(e => e.Brand).HasMaxLength(Equipment.BrandMaxLength).IsRequired();
        builder.Property(e => e.Model).HasMaxLength(Equipment.ModelMaxLength).IsRequired();
        builder.Property(e => e.IsModelConfirmed).IsRequired();
        builder.Property(e => e.SerialNumber).HasMaxLength(Equipment.SerialNumberMaxLength).IsRequired();
        builder.HasIndex(e => new { e.CompanyId, e.SerialNumber }).IsUnique().HasDatabaseName("UQ_Equipment_Company_Serial");
        builder.Property(e => e.InternalCode).HasMaxLength(Equipment.InternalCodeMaxLength);
        builder.Property(e => e.Notes).HasMaxLength(Equipment.NotesMaxLength);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(e => e.UpdatedAt).HasPrecision(0);
        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
