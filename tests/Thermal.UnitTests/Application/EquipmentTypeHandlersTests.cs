using NSubstitute;
using Thermal.Application.Abstractions;
using Thermal.Application.EquipmentTypes;
using Thermal.Application.EquipmentTypes.Create;
using Thermal.Application.EquipmentTypes.GetById;
using Thermal.Application.EquipmentTypes.Update;
using Thermal.Application.Exceptions;
using Thermal.Domain.Common;
using Thermal.Domain.EquipmentTypes;

namespace Thermal.UnitTests.Application;

/// <summary>Casos de uso de HU-02 con los puertos sustituidos por dobles (NSubstitute).</summary>
[Trait("Story", "HU-02")]
public sealed class EquipmentTypeHandlersTests
{
    private readonly IEquipmentTypeRepository _repository = Substitute.For<IEquipmentTypeRepository>();
    private readonly IEquipmentTypeReadStore _readStore = Substitute.For<IEquipmentTypeReadStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateEquipmentTypeCommandHandler CreateHandler => new(_repository, _unitOfWork);

    private UpdateEquipmentTypeCommandHandler UpdateHandler => new(_repository, _unitOfWork);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static UpdateEquipmentTypeCommand UpdateCommand(
        int id = 2, string name = "Congeladora", decimal? maxTemperatureC = -8.00m, bool isActive = true,
        byte[]? expectedVersion = null) =>
        new(id, name, LimitMode: null, maxTemperatureC, ToleranceK: null, MinMeasurementPoints: null,
            MinSessionDurationMinutes: null, Description: null, isActive, expectedVersion);

    private EquipmentType GivenStoredEquipmentType(int id = 2)
    {
        var equipmentType = new EquipmentType("Congeladora", LimitMode.Maximum, -5.00m, null, 9, 60, null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(equipmentType);
        return equipmentType;
    }

    [Fact] // HU-02 · Scenario: Registrar un tipo de equipo con límite definido
    public async Task Create_ValidCommand_AddsAndSavesWithBaseDuration()
    {
        var result = await CreateHandler.HandleAsync(
            new CreateEquipmentTypeCommand("Ultracongeladora", LimitMode: null, -60.0m, ToleranceK: null,
                MinMeasurementPoints: null, MinSessionDurationMinutes: null, Description: null),
            Token);

        result.Name.Should().Be("Ultracongeladora");
        result.MaxTemperatureC.Should().Be(-60.00m);
        result.IsLimitDefined.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.MinSessionDurationMinutes.Should().Be(60);
        result.LimitMode.Should().Be(LimitMode.Maximum);
        result.MinMeasurementPoints.Should().Be(9);
        _repository.Received(1).Add(Arg.Is<EquipmentType>(e => e.Name == "Ultracongeladora"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Scenario: Rechazar un nombre de tipo duplicado
    public async Task Create_DuplicateName_ThrowsConflictAndDoesNotSave()
    {
        _repository.NameExistsAsync("Congeladora", null, Arg.Any<CancellationToken>()).Returns(true);

        var create = () => CreateHandler.HandleAsync(
            new CreateEquipmentTypeCommand("Congeladora", null, null, null, null, null, null), Token);

        await create.Should().ThrowAsync<ConflictException>()
            .WithMessage("Ya existe el tipo de equipo Congeladora");
        _repository.DidNotReceive().Add(Arg.Any<EquipmentType>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Scenario: Rechazar un límite con formato inválido (sin tocar la base)
    public async Task Create_InvalidLimit_ThrowsBeforeQueryingTheDatabase()
    {
        var create = () => CreateHandler.HandleAsync(
            new CreateEquipmentTypeCommand("Congeladora", null, -5.123m, null, null, null, null), Token);

        await create.Should().ThrowAsync<DomainValidationException>();
        await _repository.DidNotReceive()
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Scenario: Editar el límite sin afectar sesiones registradas (edición del catálogo)
    public async Task Update_ExistingType_ChangesLimitAndSaves()
    {
        var freezer = GivenStoredEquipmentType();

        var result = await UpdateHandler.HandleAsync(UpdateCommand(maxTemperatureC: -8.00m), Token);

        result.MaxTemperatureC.Should().Be(-8.00m);
        freezer.MaxTemperatureC.Should().Be(-8.00m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Scenario: Desactivar un tipo con equipos asociados
    public async Task Update_WithIsActiveFalse_DeactivatesTheType()
    {
        var keeper = GivenStoredEquipmentType();

        await UpdateHandler.HandleAsync(UpdateCommand(isActive: false), Token);

        keeper.IsActive.Should().BeFalse();
    }

    [Fact] // HU-02 · Regla D-05 y D-06: tipo con banda y 27 puntos (incubadora de más de 50 L)
    public async Task Create_BandWith27Points_KeepsTheCriterion()
    {
        var result = await CreateHandler.HandleAsync(
            new CreateEquipmentTypeCommand("Incubadora grande", LimitMode.Band, null, 0.5m, 27, null, null), Token);

        result.LimitMode.Should().Be(LimitMode.Band);
        result.ToleranceK.Should().Be(0.5m);
        result.IsLimitDefined.Should().BeTrue();
        result.MinMeasurementPoints.Should().Be(27);
    }

    [Fact] // HU-02 · Regla D-05: PUT de máximo a banda
    public async Task Update_ToBand_ReplacesTheLimitCriterion()
    {
        var type = GivenStoredEquipmentType();

        await UpdateHandler.HandleAsync(
            new UpdateEquipmentTypeCommand(2, "Congeladora", LimitMode.Band, null, 1.0m, 9, null, null, true, null), Token);

        type.LimitMode.Should().Be(LimitMode.Band);
        type.MaxTemperatureC.Should().BeNull();
        type.ToleranceK.Should().Be(1.0m);
    }

    [Fact] // HU-02 · Regla: PUT sobre un tipo inexistente responde 404
    public async Task Update_UnknownId_ThrowsNotFound()
    {
        var update = () => UpdateHandler.HandleAsync(UpdateCommand(id: 99), Token);

        await update.Should().ThrowAsync<NotFoundException>().WithMessage("No existe el tipo de equipo 99.");
    }

    [Fact] // HU-02 · Scenario: Rechazar un nombre de tipo duplicado (al renombrar)
    public async Task Update_RenameToExistingName_ThrowsConflict()
    {
        GivenStoredEquipmentType();
        _repository.NameExistsAsync("Refrigeradora", 2, Arg.Any<CancellationToken>()).Returns(true);

        var update = () => UpdateHandler.HandleAsync(UpdateCommand(name: "Refrigeradora"), Token);

        await update.Should().ThrowAsync<ConflictException>().WithMessage("Ya existe el tipo de equipo Refrigeradora");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Regla: If-Match con una versión desactualizada responde 412
    public async Task Update_WithStaleVersion_ThrowsConcurrencyConflictAndDoesNotSave()
    {
        var freezer = GivenStoredEquipmentType();
        byte[] staleVersion = [0, 0, 0, 0, 0, 0, 7, 209];
        _repository
            .When(r => r.EnsureVersion(freezer, staleVersion))
            .Do(_ => throw new ConcurrencyConflictException("El tipo de equipo fue modificado por otro usuario."));

        var update = () => UpdateHandler.HandleAsync(UpdateCommand(expectedVersion: staleVersion), Token);

        await update.Should().ThrowAsync<ConcurrencyConflictException>();
        freezer.MaxTemperatureC.Should().Be(-5.00m);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Regla: sin If-Match no se exige versión
    public async Task Update_WithoutIfMatch_DoesNotCheckVersion()
    {
        GivenStoredEquipmentType();

        await UpdateHandler.HandleAsync(UpdateCommand(expectedVersion: null), Token);

        _repository.DidNotReceive().EnsureVersion(Arg.Any<EquipmentType>(), Arg.Any<byte[]>());
    }

    [Fact] // HU-02 · Regla: GET de un tipo inexistente responde 404
    public async Task GetById_UnknownId_ThrowsNotFound()
    {
        var handler = new GetEquipmentTypeByIdQueryHandler(_readStore);

        var get = () => handler.HandleAsync(new GetEquipmentTypeByIdQuery(99), Token);

        await get.Should().ThrowAsync<NotFoundException>();
    }
}
