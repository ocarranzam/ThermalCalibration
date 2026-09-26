using NSubstitute;
using Thermal.Application.Abstractions;
using Thermal.Application.Companies;
using Thermal.Application.Equipments;
using Thermal.Application.EquipmentTypes;
using Thermal.Application.Exceptions;
using Thermal.Domain.Common;
using Thermal.Domain.Companies;
using Thermal.Domain.Equipments;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.UnitTests.Application;

/// <summary>Casos de uso de HU-01 con los puertos sustituidos por dobles (NSubstitute).</summary>
[Trait("Story", "HU-01")]
public sealed class CompanyAndEquipmentHandlersTests
{
    private const string TaxId = "20100070970";

    private readonly ICompanyRepository _companies = Substitute.For<ICompanyRepository>();
    private readonly ICompanyReadStore _companyReadStore = Substitute.For<ICompanyReadStore>();
    private readonly IEquipmentTypeRepository _equipmentTypes = Substitute.For<IEquipmentTypeRepository>();
    private readonly IEquipmentRepository _equipment = Substitute.For<IEquipmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private CreateCompanyCommandHandler CreateCompany => new(_companies, _unitOfWork);

    private CreateEquipmentCommandHandler CreateEquipment => new(_companies, _equipmentTypes, _equipment, _unitOfWork);

    private UpdateEquipmentCommandHandler UpdateEquipment => new(_equipmentTypes, _equipment, _unitOfWork);

    private static CreateEquipmentCommand EquipmentCommand(int equipmentTypeId = 2, bool? isModelConfirmed = null) =>
        new(1, equipmentTypeId, "Haier", "HBF-205", isModelConfirmed, "SN-88231", null, null);

    private void GivenCompany(int id = 1) =>
        _companies.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Company(TaxId, "Laboratorios Andinos S.A.C.", null, null, null, null));

    private EquipmentType GivenEquipmentType(int id = 2, string name = "Congeladora", bool isActive = true)
    {
        var type = new EquipmentType(name, LimitMode.Range, null, -5.00m, null, 9, 60, null);
        if (!isActive)
        {
            type.Deactivate();
        }

        _equipmentTypes.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(type);
        return type;
    }

    [Fact] // HU-01 · Scenario: Registrar una empresa nueva
    public async Task CreateCompany_New_AddsActiveCompanyAndSaves()
    {
        var result = await CreateCompany.HandleAsync(
            new CreateCompanyCommand(TaxId, "Laboratorios Andinos S.A.C.", null, null, null, null), Token);

        result.TaxId.Should().Be(TaxId);
        result.IsActive.Should().BeTrue();
        _companies.Received(1).Add(Arg.Is<Company>(c => c.TaxId == TaxId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-01 · Scenario: Rechazar un RUC duplicado (y ofrecer abrir la empresa existente)
    public async Task CreateCompany_DuplicateTaxId_ThrowsConflictWithExistingId()
    {
        _companies.FindIdByTaxIdAsync(TaxId, Arg.Any<CancellationToken>()).Returns(7);

        var create = () => CreateCompany.HandleAsync(new CreateCompanyCommand(TaxId, "Otra S.A.", null, null, null, null), Token);

        (await create.Should().ThrowAsync<ConflictException>().WithMessage("Ya existe una empresa con el RUC 20100070970"))
            .Which.ExistingId.Should().Be(7);
        _companies.DidNotReceive().Add(Arg.Any<Company>());
    }

    [Fact] // HU-01 · Scenario Outline: Rechazar un RUC con formato inválido (sin consultar la base)
    public async Task CreateCompany_InvalidTaxId_ThrowsBeforeQuerying()
    {
        var create = () => CreateCompany.HandleAsync(new CreateCompanyCommand("20100070971", "X", null, null, null, null), Token);

        await create.Should().ThrowAsync<DomainValidationException>();
        await _companies.DidNotReceive().FindIdByTaxIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact] // HU-01 · Regla: la empresa que no existe responde 404 al registrar un equipo
    public async Task CreateEquipment_UnknownCompany_ThrowsNotFound()
    {
        var create = () => CreateEquipment.HandleAsync(EquipmentCommand(), Token);

        await create.Should().ThrowAsync<NotFoundException>().WithMessage("No existe la empresa 1.");
    }

    [Fact] // HU-01 · Scenario: Registrar un equipo asociado a la empresa (modelo confirmado por defecto)
    public async Task CreateEquipment_Valid_AddsAndReturnsTypeName()
    {
        GivenCompany();
        GivenEquipmentType();

        var result = await CreateEquipment.HandleAsync(EquipmentCommand(), Token);

        result.EquipmentTypeName.Should().Be("Congeladora");
        result.IsModelConfirmed.Should().BeTrue();
        _equipment.Received(1).Add(Arg.Is<Equipment>(e => e.SerialNumber == "SN-88231" && e.CompanyId == 1));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-01 · Scenario: Rechazar un número de serie duplicado en la misma empresa
    public async Task CreateEquipment_DuplicateSerial_ThrowsConflict()
    {
        GivenCompany();
        GivenEquipmentType();
        _equipment.SerialExistsAsync(1, "SN-88231", null, Arg.Any<CancellationToken>()).Returns(true);

        var create = () => CreateEquipment.HandleAsync(EquipmentCommand(), Token);

        await create.Should().ThrowAsync<ConflictException>().WithMessage("La empresa ya tiene un equipo con la serie SN-88231");
        _equipment.DidNotReceive().Add(Arg.Any<Equipment>());
    }

    [Fact] // HU-02 · Scenario: Desactivar un tipo con equipos asociados (no se ofrece para equipos nuevos)
    [Trait("Story", "HU-02")]
    public async Task CreateEquipment_InactiveType_IsRejected()
    {
        GivenCompany();
        GivenEquipmentType(isActive: false);

        var create = () => CreateEquipment.HandleAsync(EquipmentCommand(), Token);

        (await create.Should().ThrowAsync<DomainValidationException>().WithMessage("El tipo de equipo Congeladora está desactivado"))
            .Which.Property.Should().Be(nameof(Equipment.EquipmentTypeId));
    }

    [Fact] // HU-01 · Scenario Outline: Datos obligatorios del equipo (tipo de equipo inexistente)
    public async Task CreateEquipment_UnknownType_IsRejected()
    {
        GivenCompany();

        var create = () => CreateEquipment.HandleAsync(EquipmentCommand(equipmentTypeId: 99), Token);

        await create.Should().ThrowAsync<DomainValidationException>().WithMessage("No existe el tipo de equipo 99");
    }

    [Fact] // HU-01 · Regla: un equipo conserva su tipo aunque este se haya desactivado después
    public async Task UpdateEquipment_SameInactiveType_IsAllowed()
    {
        GivenEquipmentType(isActive: false);
        var equipment = new Equipment(1, 2, "Haier", "HBF-205", false, "SN-88231", null, null);
        _equipment.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(equipment);

        var result = await UpdateEquipment.HandleAsync(
            new UpdateEquipmentCommand(5, 2, "Haier", "HBF-205", true, "SN-88231", "CONG-03", null, true, null), Token);

        result.IsModelConfirmed.Should().BeTrue();
        equipment.InternalCode.Should().Be("CONG-03");
        await _equipment.Received(1).SerialExistsAsync(1, "SN-88231", 5, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-01 · Regla: la búsqueda ignora filtros vacíos y recorta espacios
    public async Task SearchCompanies_NormalizesFilters()
    {
        await new SearchCompaniesQueryHandler(_companyReadStore).HandleAsync(new SearchCompaniesQuery(" ", " Andinos "), Token);

        await _companyReadStore.Received(1).SearchAsync(null, "Andinos", Arg.Any<CancellationToken>());
    }
}
