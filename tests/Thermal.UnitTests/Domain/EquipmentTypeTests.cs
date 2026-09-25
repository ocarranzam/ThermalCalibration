using Thermal.Domain.Common;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.UnitTests.Domain;

/// <summary>
/// Agregado <see cref="EquipmentType"/> frente a los criterios Gherkin de HU-02
/// (docs/diagrams/gherkin/HU-02-gestionar-tipos-de-equipo-y-limite-maximo.feature).
/// </summary>
[Trait("Story", "HU-02")]
public sealed class EquipmentTypeTests
{
    private static EquipmentType NewEquipmentType(
        string name = "Congeladora",
        decimal? maxTemperatureC = -5.00m,
        int minSessionDurationMinutes = EquipmentType.BaseSessionDurationMinutes,
        string? description = null) =>
        new(name, maxTemperatureC, minSessionDurationMinutes, description);

    [Fact] // HU-02 · Scenario: Registrar un tipo de equipo con límite definido
    public void Create_WithDefinedLimit_IsActiveWithLimitAndBaseDuration()
    {
        var equipmentType = NewEquipmentType("Ultracongeladora", maxTemperatureC: -60.0m);

        equipmentType.Name.Should().Be("Ultracongeladora");
        equipmentType.IsActive.Should().BeTrue();
        equipmentType.MaxTemperatureC.Should().Be(-60.00m);
        equipmentType.MaxTemperature.IsDefined.Should().BeTrue();
        equipmentType.MinSessionDurationMinutes.Should().Be(60);
    }

    [Fact] // HU-02 · Scenario: Registrar un tipo de equipo con límite pendiente
    public void Create_WithoutLimit_LeavesLimitPending()
    {
        var equipmentType = NewEquipmentType("Cámara de vacunas", maxTemperatureC: null);

        equipmentType.IsActive.Should().BeTrue();
        equipmentType.MaxTemperatureC.Should().BeNull();
        equipmentType.MaxTemperature.Should().Be(TemperatureLimit.Pending);
        equipmentType.MaxTemperature.IsDefined.Should().BeFalse();
    }

    [Fact] // HU-02 · Scenario: Rechazar un límite con formato inválido
    public void Create_WithThreeDecimalLimit_IsRejected()
    {
        var create = () => NewEquipmentType(maxTemperatureC: -5.123m);

        create.Should().Throw<DomainValidationException>()
            .Which.Should().Match<DomainValidationException>(e =>
                e.Property == nameof(EquipmentType.MaxTemperatureC)
                && e.Message == "El límite admite como máximo 2 decimales");
    }

    [Fact] // HU-02 · Scenario: Exigir una duración mínima mayor para un tipo de equipo
    public void ChangeMinSessionDuration_To120Minutes_RaisesTheMinimum()
    {
        var incubator = NewEquipmentType("Incubadora", maxTemperatureC: null);

        incubator.ChangeMinSessionDuration(120);

        incubator.MinSessionDurationMinutes.Should().Be(120);
    }

    [Fact] // HU-02 · Scenario: Rechazar una duración mínima menor que la base
    public void ChangeMinSessionDuration_BelowBase_IsRejectedAndKeepsValue()
    {
        var freezer = NewEquipmentType();

        var change = () => freezer.ChangeMinSessionDuration(45);

        change.Should().Throw<DomainValidationException>()
            .WithMessage("La duración mínima no puede ser menor que 60 minutos")
            .Which.Property.Should().Be(nameof(EquipmentType.MinSessionDurationMinutes));
        freezer.MinSessionDurationMinutes.Should().Be(60);
    }

    [Theory] // HU-02 · Regla: duración mínima entre 60 y 43 200 min (CK_EquipmentType_MinDuration, RN-04)
    [InlineData(59, false)]
    [InlineData(60, true)]
    [InlineData(43200, true)]
    [InlineData(43201, false)]
    public void Create_MinSessionDuration_RespectsDatabaseRange(int minutes, bool isValid)
    {
        var create = () => NewEquipmentType(minSessionDurationMinutes: minutes);

        if (isValid)
        {
            create.Should().NotThrow();
        }
        else
        {
            create.Should().Throw<DomainValidationException>()
                .Which.Property.Should().Be(nameof(EquipmentType.MinSessionDurationMinutes));
        }
    }

    [Fact] // HU-02 · Scenario: Editar el límite sin afectar sesiones registradas (lado del catálogo)
    public void ChangeLimit_ReplacesTheCatalogLimit()
    {
        // La copia del límite en MeasurementSession (RN-08) se probará cuando exista ese agregado.
        var freezer = NewEquipmentType(maxTemperatureC: -5.00m);

        freezer.ChangeLimit(TemperatureLimit.Create(-8.00m));

        freezer.MaxTemperatureC.Should().Be(-8.00m);
    }

    [Fact] // HU-02 · Scenario: Definir un límite que estaba pendiente
    public void ChangeLimit_FromPendingToDefined_DefinesTheLimit()
    {
        var fridge = NewEquipmentType("Refrigeradora", maxTemperatureC: null);

        fridge.ChangeLimit(TemperatureLimit.Create(8.00m));

        fridge.MaxTemperature.IsDefined.Should().BeTrue();
        fridge.MaxTemperatureC.Should().Be(8.00m);
    }

    [Fact] // HU-02 · Regla: un límite definido puede volver a quedar pendiente (PUT sin maxTemperatureC)
    public void ChangeLimit_ToPending_ClearsTheLimit()
    {
        var freezer = NewEquipmentType();

        freezer.ChangeLimit(TemperatureLimit.Pending);

        freezer.MaxTemperatureC.Should().BeNull();
    }

    [Fact] // HU-02 · Scenario: Desactivar un tipo con equipos asociados
    public void Deactivate_MarksTheTypeInactive_AndActivateRestoresIt()
    {
        var keeper = NewEquipmentType("Conservadora", maxTemperatureC: null);

        keeper.Deactivate();
        keeper.IsActive.Should().BeFalse();

        keeper.Activate();
        keeper.IsActive.Should().BeTrue();
    }

    [Theory] // HU-02 · Regla: nombre obligatorio, sin espacios en los extremos (CK_EquipmentType_Name)
    [InlineData("", "El nombre del tipo de equipo es obligatorio")]
    [InlineData("   ", "El nombre del tipo de equipo es obligatorio")]
    [InlineData(" Congeladora", "El nombre no puede empezar ni terminar con espacios")]
    [InlineData("Congeladora ", "El nombre no puede empezar ni terminar con espacios")]
    public void Create_WithInvalidName_IsRejected(string name, string expectedMessage)
    {
        var create = () => NewEquipmentType(name);

        create.Should().Throw<DomainValidationException>()
            .WithMessage(expectedMessage)
            .Which.Property.Should().Be(nameof(EquipmentType.Name));
    }

    [Fact] // HU-02 · Regla: nombre de hasta 100 caracteres (NVARCHAR(100))
    public void Create_NameLength_IsLimitedTo100Characters()
    {
        var atLimit = () => NewEquipmentType(new string('A', EquipmentType.NameMaxLength));
        var overLimit = () => NewEquipmentType(new string('A', EquipmentType.NameMaxLength + 1));

        atLimit.Should().NotThrow();
        overLimit.Should().Throw<DomainValidationException>()
            .WithMessage("El nombre admite como máximo 100 caracteres");
    }

    [Fact] // HU-02 · Regla: al renombrar se aplican las mismas validaciones que al crear
    public void Rename_WithInvalidName_IsRejectedAndKeepsName()
    {
        var freezer = NewEquipmentType();

        var rename = () => freezer.Rename(" ");

        rename.Should().Throw<DomainValidationException>();
        freezer.Name.Should().Be("Congeladora");
    }

    [Fact] // HU-02 · Regla: descripción de hasta 500 caracteres (NVARCHAR(500))
    public void Describe_LongerThan500Characters_IsRejected()
    {
        var freezer = NewEquipmentType();

        var describe = () => freezer.Describe(new string('x', EquipmentType.DescriptionMaxLength + 1));

        describe.Should().Throw<DomainValidationException>()
            .Which.Property.Should().Be(nameof(EquipmentType.Description));
        freezer.Description.Should().BeNull();
    }
}
