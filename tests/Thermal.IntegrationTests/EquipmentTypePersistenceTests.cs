using Thermal.Application.Exceptions;
using Thermal.Domain.EquipmentTypes;
using Thermal.Infrastructure.Persistence;

namespace Thermal.IntegrationTests;

/// <summary>
/// Persistencia de <see cref="EquipmentType"/> contra el esquema real (HU-02): lo que las pruebas
/// unitarias no pueden comprobar, como los valores que genera la base y sus restricciones.
/// Cada prueba usa nombres únicos, porque todas comparten la misma base.
/// </summary>
[Trait("Story", "HU-02")]
public sealed class EquipmentTypePersistenceTests(SqlServerFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..40];

    private async Task<int> InsertAsync(string name, decimal? maxTemperatureC = -5.00m)
    {
        await using var context = database.CreateDbContext();
        var equipmentType = new EquipmentType(name, LimitMode.Maximum, maxTemperatureC, null, 9, 60, null);
        new EquipmentTypeRepository(context).Add(equipmentType);
        await new UnitOfWork(context).SaveChangesAsync(Token);
        return equipmentType.Id;
    }

    [Fact] // HU-02 · Scenario: Registrar un tipo de equipo con límite definido (valores que asigna la base)
    public async Task Add_AssignsIdCreatedAtAndRowVersionFromTheDatabase()
    {
        await using var context = database.CreateDbContext();
        var equipmentType = new EquipmentType(UniqueName("Ultracongeladora"), LimitMode.Maximum, -60.00m, null, 9, 60, null);

        new EquipmentTypeRepository(context).Add(equipmentType);
        await new UnitOfWork(context).SaveChangesAsync(Token);

        equipmentType.Id.Should().BePositive();
        equipmentType.CreatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        equipmentType.RowVersion.Should().HaveCount(8);
        equipmentType.UpdatedAt.Should().BeNull();
    }

    [Fact] // HU-02 · Scenario: Rechazar un nombre de tipo duplicado (UQ_EquipmentType_Name, sin distinguir mayúsculas)
    public async Task Add_DuplicateNameWithDifferentCase_ThrowsConflict()
    {
        var name = UniqueName("Congeladora");
        await InsertAsync(name);

        var insertDuplicate = () => InsertAsync(name.ToUpperInvariant());

        await insertDuplicate.Should().ThrowAsync<ConflictException>();
    }

    [Fact] // HU-02 · Scenario: Rechazar un nombre de tipo duplicado (consulta previa del handler)
    public async Task NameExists_IgnoresCaseAndExcludesTheSameType()
    {
        var name = UniqueName("Conservadora");
        var id = await InsertAsync(name);
        await using var context = database.CreateDbContext();
        var repository = new EquipmentTypeRepository(context);

        (await repository.NameExistsAsync(name.ToLowerInvariant(), excludingId: null, Token)).Should().BeTrue();
        (await repository.NameExistsAsync(name, excludingId: id, Token)).Should().BeFalse();
    }

    [Fact] // HU-02 · Scenario: Editar el límite sin afectar sesiones registradas (UpdatedAt y nueva versión)
    public async Task Update_SetsUpdatedAtAndChangesRowVersion()
    {
        var id = await InsertAsync(UniqueName("Congeladora"));
        await using var context = database.CreateDbContext();
        var equipmentType = (await new EquipmentTypeRepository(context).GetByIdAsync(id, Token))!;
        var previousVersion = equipmentType.RowVersion;

        equipmentType.ChangeLimit(TemperatureLimit.Create(-8.00m));
        await new UnitOfWork(context).SaveChangesAsync(Token);

        equipmentType.UpdatedAt.Should().NotBeNull();
        equipmentType.UpdatedAt!.Value.Millisecond.Should().Be(0, "la columna es DATETIMEOFFSET(0)");
        equipmentType.RowVersion.Should().NotEqual(previousVersion);
    }

    [Fact] // HU-02 · Regla: una edición inmediata no queda con UpdatedAt anterior a CreatedAt (ambos redondeados)
    public async Task Update_RightAfterCreate_UpdatedAtIsNotBeforeCreatedAt()
    {
        var id = await InsertAsync(UniqueName("Congeladora"));
        await using var context = database.CreateDbContext();
        var equipmentType = (await new EquipmentTypeRepository(context).GetByIdAsync(id, Token))!;

        equipmentType.Describe("Editado en el mismo segundo");
        await new UnitOfWork(context).SaveChangesAsync(Token);

        equipmentType.UpdatedAt.Should().BeOnOrAfter(equipmentType.CreatedAt);
    }

    [Fact] // HU-02 · Regla: dos administradores editan a la vez; el segundo recibe 412 (RowVersion)
    public async Task ConcurrentUpdate_WithTheSameReadVersion_SecondSaveThrowsConcurrencyConflict()
    {
        var id = await InsertAsync(UniqueName("Refrigeradora"), maxTemperatureC: null);
        await using var firstContext = database.CreateDbContext();
        await using var secondContext = database.CreateDbContext();
        var first = (await new EquipmentTypeRepository(firstContext).GetByIdAsync(id, Token))!;
        var second = (await new EquipmentTypeRepository(secondContext).GetByIdAsync(id, Token))!;
        var versionReadBySecond = second.RowVersion;

        first.ChangeLimit(TemperatureLimit.Create(8.00m));
        await new UnitOfWork(firstContext).SaveChangesAsync(Token);

        new EquipmentTypeRepository(secondContext).EnsureVersion(second, versionReadBySecond);
        second.ChangeLimit(TemperatureLimit.Create(6.00m));
        var saveSecond = () => new UnitOfWork(secondContext).SaveChangesAsync(Token);

        await saveSecond.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact] // HU-02 · Regla: If-Match con una versión ya reemplazada se rechaza antes de editar
    public async Task EnsureVersion_WithStaleVersion_ThrowsConcurrencyConflict()
    {
        var id = await InsertAsync(UniqueName("Incubadora"));
        await using var context = database.CreateDbContext();
        var repository = new EquipmentTypeRepository(context);
        var equipmentType = (await repository.GetByIdAsync(id, Token))!;

        var ensure = () => repository.EnsureVersion(equipmentType, [0, 0, 0, 0, 0, 0, 0, 1]);

        ensure.Should().Throw<ConcurrencyConflictException>();
    }

    [Fact] // HU-02 · Regla D-05 y D-06: banda y 27 puntos se guardan y se leen (LimitMode como texto, TINYINT)
    public async Task Add_BandTypeWith27Points_RoundTrips()
    {
        int id;
        await using (var context = database.CreateDbContext())
        {
            var incubator = new EquipmentType(UniqueName("Incubadora"), LimitMode.Band, null, 0.5m, 27, 60, null);
            new EquipmentTypeRepository(context).Add(incubator);
            await new UnitOfWork(context).SaveChangesAsync(Token);
            id = incubator.Id;
        }

        await using var readContext = database.CreateDbContext();
        var dto = await new EquipmentTypeReadStore(readContext).GetByIdAsync(id, Token);

        dto!.LimitMode.Should().Be(LimitMode.Band);
        dto.ToleranceK.Should().Be(0.50m);
        dto.MaxTemperatureC.Should().BeNull();
        dto.IsLimitDefined.Should().BeTrue();
        dto.MinMeasurementPoints.Should().Be(27);
    }

    [Fact] // HU-02 · Scenario: Registrar un tipo de equipo con límite pendiente (lectura del catálogo inicial)
    public async Task ReadStore_ReturnsSeededTypesWithDefinedAndPendingLimits()
    {
        await using var context = database.CreateDbContext();
        var readStore = new EquipmentTypeReadStore(context);

        var fridge = await readStore.GetByIdAsync(1, Token);
        var freezer = await readStore.GetByIdAsync(2, Token);

        fridge.Should().NotBeNull();
        fridge!.Name.Should().Be("Refrigeradora");
        fridge.IsLimitDefined.Should().BeFalse();
        freezer!.MaxTemperatureC.Should().Be(-5.00m);
        freezer.IsLimitDefined.Should().BeTrue();
        freezer.Version.Should().NotBeNullOrEmpty();
        var incubator = await readStore.GetByIdAsync(4, Token);
        incubator!.LimitMode.Should().Be(LimitMode.Band);
        incubator.MinMeasurementPoints.Should().Be(27);
    }
}
