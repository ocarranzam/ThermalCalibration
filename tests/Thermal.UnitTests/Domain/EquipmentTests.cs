using Thermal.Domain.Common;
using Thermal.Domain.Equipments;

namespace Thermal.UnitTests.Domain;

/// <summary>Equipo bajo prueba de una empresa (HU-01).</summary>
[Trait("Story", "HU-01")]
public sealed class EquipmentTests
{
    private static Equipment NewEquipment(
        string brand = "Haier", string model = "HBF-205", bool isModelConfirmed = true, string serialNumber = "SN-88231") =>
        new(companyId: 1, equipmentTypeId: 2, brand, model, isModelConfirmed, serialNumber, internalCode: null, notes: null);

    [Fact] // HU-01 · Scenario: Registrar un equipo asociado a la empresa
    public void Create_ValidEquipment_IsActiveAndConfirmed()
    {
        var equipment = NewEquipment();

        equipment.CompanyId.Should().Be(1);
        equipment.EquipmentTypeId.Should().Be(2);
        equipment.Brand.Should().Be("Haier");
        equipment.Model.Should().Be("HBF-205");
        equipment.SerialNumber.Should().Be("SN-88231");
        equipment.IsModelConfirmed.Should().BeTrue();
        equipment.IsActive.Should().BeTrue();
    }

    [Theory] // HU-01 · Scenario Outline: Datos obligatorios del equipo
    [InlineData("", "HBF-205", "SN-88231", nameof(Equipment.Brand), "La marca es obligatoria")]
    [InlineData("Haier", " ", "SN-88231", nameof(Equipment.Model), "El modelo es obligatorio")]
    [InlineData("Haier", "HBF-205", "", nameof(Equipment.SerialNumber), "El número de serie es obligatorio")]
    public void Create_MissingRequiredData_IsRejected(string brand, string model, string serial, string property, string message)
    {
        var create = () => NewEquipment(brand, model, serialNumber: serial);

        create.Should().Throw<DomainValidationException>().WithMessage(message).Which.Property.Should().Be(property);
    }

    [Fact] // HU-01 · Scenario: Registrar un equipo con el modelo inferido
    public void Create_InferredModel_IsPendingConfirmation()
    {
        var chamber = NewEquipment("Memmert", "TTC256", isModelConfirmed: false, serialNumber: "DATA-1");

        chamber.IsModelConfirmed.Should().BeFalse();
        chamber.Model.Should().Be("TTC256");
    }

    [Fact] // HU-01 · Scenario: Registrar un equipo con el modelo inferido (confirmación posterior en la placa)
    public void ConfirmModel_SetsModelAndConfirms()
    {
        var chamber = NewEquipment("Memmert", "TTC256", isModelConfirmed: false);

        chamber.ConfirmModel("TTC256");

        chamber.IsModelConfirmed.Should().BeTrue();
        chamber.Brand.Should().Be("Memmert");
    }

    [Fact] // HU-01 · Regla: longitudes máximas y opcionales vacíos como null
    public void Describe_TrimsAndValidatesLength()
    {
        var equipment = NewEquipment();

        equipment.Describe(" CONG-03 ", " ");
        equipment.InternalCode.Should().Be("CONG-03");
        equipment.Notes.Should().BeNull();

        var tooLong = () => equipment.Describe(new string('C', Equipment.InternalCodeMaxLength + 1), null);
        tooLong.Should().Throw<DomainValidationException>().WithMessage("El código interno admite como máximo 50 caracteres");
    }

    [Fact] // HU-01 · Regla: una edición inválida no cambia el equipo
    public void ChangeSerialNumber_Blank_IsRejectedAndKeepsSerial()
    {
        var equipment = NewEquipment();

        var change = () => equipment.ChangeSerialNumber(" ");

        change.Should().Throw<DomainValidationException>();
        equipment.SerialNumber.Should().Be("SN-88231");
    }
}
