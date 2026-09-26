using Microsoft.EntityFrameworkCore;
using Thermal.Application.Companies;
using Thermal.Application.Equipments;
using Thermal.Application.Exceptions;
using Thermal.Domain.Companies;
using Thermal.Domain.Equipments;

namespace Thermal.Infrastructure.Persistence;

internal sealed class CompanyRepository(ThermalDbContext context) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Companies.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<int?> FindIdByTaxIdAsync(string taxId, CancellationToken cancellationToken) =>
        await context.Companies.Where(c => c.TaxId == taxId).Select(c => (int?)c.Id).SingleOrDefaultAsync(cancellationToken);

    public void Add(Company company) => context.Companies.Add(company);

    public void EnsureVersion(Company company, byte[] expectedRowVersion) =>
        RowVersions.Ensure(context, company, expectedRowVersion, "La empresa fue modificada por otro usuario. Vuelva a leerla antes de guardar.");
}

internal sealed class CompanyReadStore(ThermalDbContext context) : ICompanyReadStore
{
    private const int MaxSearchResults = 100;

    public async Task<CompanyDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await Project(context.Companies.AsNoTracking().Where(c => c.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CompanyDto>> SearchAsync(string? taxId, string? name, CancellationToken cancellationToken)
    {
        var query = context.Companies.AsNoTracking();
        if (taxId is not null)
        {
            query = query.Where(c => c.TaxId == taxId);
        }

        if (name is not null)
        {
            query = query.Where(c => c.Name.Contains(name));
        }

        return await Project(query.OrderBy(c => c.Name).Take(MaxSearchResults)).ToListAsync(cancellationToken);
    }

    private static IQueryable<CompanyDto> Project(IQueryable<Company> query) => query.Select(c => new CompanyDto(
        c.Id, c.TaxId, c.Name, c.ContactName, c.Phone, c.Email, c.Address, c.IsActive, c.CreatedAt, c.UpdatedAt,
        Convert.ToBase64String(c.RowVersion)));
}

internal sealed class EquipmentRepository(ThermalDbContext context) : IEquipmentRepository
{
    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Equipment.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> SerialExistsAsync(int companyId, string serialNumber, int? excludingId, CancellationToken cancellationToken) =>
        context.Equipment.AnyAsync(
            e => e.CompanyId == companyId && e.SerialNumber == serialNumber && (excludingId == null || e.Id != excludingId),
            cancellationToken);

    public void Add(Equipment equipment) => context.Equipment.Add(equipment);

    public void EnsureVersion(Equipment equipment, byte[] expectedRowVersion) =>
        RowVersions.Ensure(context, equipment, expectedRowVersion, "El equipo fue modificado por otro usuario. Vuelva a leerlo antes de guardar.");
}

internal sealed class EquipmentReadStore(ThermalDbContext context) : IEquipmentReadStore
{
    public async Task<EquipmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await Project(context.Equipment.AsNoTracking().Where(e => e.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EquipmentDto>> ListByCompanyAsync(int companyId, CancellationToken cancellationToken) =>
        await Project(context.Equipment.AsNoTracking().Where(e => e.CompanyId == companyId).OrderBy(e => e.SerialNumber))
            .ToListAsync(cancellationToken);

    private IQueryable<EquipmentDto> Project(IQueryable<Equipment> query) =>
        from e in query
        join t in context.EquipmentTypes on e.EquipmentTypeId equals t.Id
        select new EquipmentDto(
            e.Id, e.CompanyId, e.EquipmentTypeId, t.Name, e.Brand, e.Model, e.IsModelConfirmed, e.SerialNumber,
            e.InternalCode, e.Notes, e.IsActive, e.CreatedAt, e.UpdatedAt, Convert.ToBase64String(e.RowVersion));
}

/// <summary>Comprobación de <c>If-Match</c> común a los repositorios con <c>RowVersion</c>.</summary>
internal static class RowVersions
{
    public static void Ensure<TEntity>(ThermalDbContext context, TEntity entity, byte[] expected, string message)
        where TEntity : class
    {
        var property = context.Entry(entity).Property<byte[]>("RowVersion");
        if (!expected.AsSpan().SequenceEqual(property.CurrentValue))
        {
            throw new ConcurrencyConflictException(message);
        }

        // Si otro usuario guarda entre esta lectura y el SaveChanges, el UPDATE no encuentra la fila (412).
        property.OriginalValue = expected;
    }
}
