using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Thermal.Application.Exceptions;
using Thermal.Domain.Companies;
using Thermal.Domain.Equipments;
using Thermal.Infrastructure.Persistence;

namespace Thermal.IntegrationTests;

/// <summary>
/// Persistencia de <see cref="Company"/> y <see cref="Equipment"/> contra el esquema real (HU-01): valores que genera la
/// base, <c>CK_Company_TaxId</c> y las restricciones de unicidad. Cada prueba genera su propio RUC válido.
/// </summary>
[Trait("Story", "HU-01")]
public sealed class CompanyAndEquipmentPersistenceTests(SqlServerFixture database)
{
    private static readonly int[] Weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
    private static int _sequence = Random.Shared.Next(1_000_000, 9_000_000);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>RUC válido y distinto en cada llamada (prefijo 20 y dígito verificador módulo 11).</summary>
    private static string NewTaxId()
    {
        var body = "20" + Interlocked.Increment(ref _sequence).ToString("D8", CultureInfo.InvariantCulture);
        var sum = Weights.Select((weight, i) => (body[i] - '0') * weight).Sum();
        return body + ((11 - (sum % 11)) % 10).ToString(CultureInfo.InvariantCulture);
    }

    private async Task<int> InsertCompanyAsync(string taxId)
    {
        await using var context = database.CreateDbContext();
        var company = new Company(taxId, $"Empresa {taxId}", null, null, null, null);
        new CompanyRepository(context).Add(company);
        await new UnitOfWork(context).SaveChangesAsync(Token);
        return company.Id;
    }

    private async Task<int> FirstEquipmentTypeIdAsync()
    {
        await using var context = database.CreateDbContext();
        return await context.EquipmentTypes.OrderBy(t => t.Id).Select(t => t.Id).FirstAsync(Token);
    }

    private async Task<Equipment> InsertEquipmentAsync(int companyId, string serialNumber)
    {
        await using var context = database.CreateDbContext();
        var equipment = new Equipment(companyId, await FirstEquipmentTypeIdAsync(), "Haier", "HBF-205", true, serialNumber, null, null);
        new EquipmentRepository(context).Add(equipment);
        await new UnitOfWork(context).SaveChangesAsync(Token);
        return equipment;
    }

    [Fact] // HU-01 · Scenario: Registrar una empresa nueva (valores que asigna la base y buscador)
    public async Task AddCompany_AssignsDatabaseValuesAndIsSearchable()
    {
        var taxId = NewTaxId();
        await using var context = database.CreateDbContext();
        var company = new Company(taxId, $"Laboratorios Andinos {taxId}", "Ana", null, "calidad@andinos.pe", null);

        new CompanyRepository(context).Add(company);
        await new UnitOfWork(context).SaveChangesAsync(Token);

        company.Id.Should().BePositive();
        company.CreatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        company.RowVersion.Should().HaveCount(8);
        await using var reader = database.CreateDbContext();
        var store = new CompanyReadStore(reader);
        (await store.SearchAsync(taxId, null, Token)).Should().ContainSingle().Which.Id.Should().Be(company.Id);
        (await store.SearchAsync(null, $"andinos {taxId}", Token)).Should().ContainSingle("sin distinguir mayúsculas");
    }

    [Fact] // HU-01 · Scenario: Rechazar un RUC duplicado (UQ_Company_TaxId y consulta previa)
    public async Task AddCompany_DuplicateTaxId_IsFoundAndRejectedByTheDatabase()
    {
        var taxId = NewTaxId();
        var id = await InsertCompanyAsync(taxId);

        await using var context = database.CreateDbContext();
        (await new CompanyRepository(context).FindIdByTaxIdAsync(taxId, Token)).Should().Be(id);

        var insertDuplicate = () => InsertCompanyAsync(taxId);
        await insertDuplicate.Should().ThrowAsync<ConflictException>();
    }

    [Theory] // HU-01 · Scenario Outline: Rechazar un RUC con formato inválido (CK_Company_TaxId, sin pasar por el dominio)
    [InlineData("20100070971")]
    [InlineData("2010007097")]
    [InlineData("30100070970")]
    [InlineData("2010007097A")]
    public async Task CheckConstraint_RejectsInvalidTaxId(string taxId)
    {
        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync(Token);
        await using var command = new SqlCommand("INSERT INTO dbo.Company (TaxId, Name) VALUES (@taxId, N'Prueba')", connection);
        command.Parameters.AddWithValue("@taxId", taxId);

        var insert = () => command.ExecuteNonQueryAsync(Token);

        (await insert.Should().ThrowAsync<SqlException>()).Which.Message.Should().Contain("CK_Company_TaxId");
    }

    [Fact] // HU-01 · Regla: editar la empresa asigna UpdatedAt y cambia la versión; una versión vieja da 412
    public async Task UpdateCompany_SetsUpdatedAtAndDetectsStaleVersion()
    {
        var id = await InsertCompanyAsync(NewTaxId());
        await using var context = database.CreateDbContext();
        var repository = new CompanyRepository(context);
        var company = (await repository.GetByIdAsync(id, Token))!;
        var version = company.RowVersion;

        repository.EnsureVersion(company, version);
        company.ChangeContact("Luis", "999 888 777", null, null);
        await new UnitOfWork(context).SaveChangesAsync(Token);

        company.UpdatedAt.Should().NotBeNull();
        company.RowVersion.Should().NotEqual(version);
        var ensureStale = () => repository.EnsureVersion(company, version);
        ensureStale.Should().Throw<ConcurrencyConflictException>();
    }

    [Fact] // HU-01 · Scenario: Registrar un equipo asociado a la empresa (historial vacío y nombre del tipo)
    public async Task AddEquipment_IsListedWithTheTypeName()
    {
        var companyId = await InsertCompanyAsync(NewTaxId());
        var equipment = await InsertEquipmentAsync(companyId, "SN-88231");

        equipment.Id.Should().BePositive();
        await using var context = database.CreateDbContext();
        var listed = await new EquipmentReadStore(context).ListByCompanyAsync(companyId, Token);
        listed.Should().ContainSingle().Which.EquipmentTypeName.Should().NotBeNullOrEmpty();
    }

    [Fact] // HU-01 · Scenario: Rechazar un número de serie duplicado en la misma empresa (UQ_Equipment_Company_Serial)
    public async Task AddEquipment_DuplicateSerialInSameCompany_IsRejected()
    {
        var companyId = await InsertCompanyAsync(NewTaxId());
        var first = await InsertEquipmentAsync(companyId, "SN-DUP");

        await using var context = database.CreateDbContext();
        var repository = new EquipmentRepository(context);
        (await repository.SerialExistsAsync(companyId, "SN-DUP", null, Token)).Should().BeTrue();
        (await repository.SerialExistsAsync(companyId, "SN-DUP", first.Id, Token)).Should().BeFalse();

        var insertDuplicate = () => InsertEquipmentAsync(companyId, "SN-DUP");
        await insertDuplicate.Should().ThrowAsync<ConflictException>();
    }

    [Fact] // HU-01 · Scenario: Permitir la misma serie en otra empresa
    public async Task AddEquipment_SameSerialInOtherCompany_IsAllowed()
    {
        var firstCompany = await InsertCompanyAsync(NewTaxId());
        var secondCompany = await InsertCompanyAsync(NewTaxId());
        await InsertEquipmentAsync(firstCompany, "SN-SHARED");

        var second = await InsertEquipmentAsync(secondCompany, "SN-SHARED");

        second.Id.Should().BePositive();
    }
}
