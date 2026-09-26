using NSubstitute;
using Thermal.Application.Abstractions;
using Thermal.Application.Exceptions;
using Thermal.Application.Settings;
using Thermal.Domain.Common;
using Thermal.Domain.Settings;

namespace Thermal.UnitTests.Application;

/// <summary>Edición de los parámetros del sistema (HU-02, D-08) con los puertos sustituidos.</summary>
[Trait("Story", "HU-02")]
public sealed class SystemSettingsHandlersTests
{
    private readonly ISystemSettingsRepository _repository = Substitute.For<ISystemSettingsRepository>();
    private readonly ISystemSettingsReadStore _readStore = Substitute.For<ISystemSettingsReadStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateSystemSettingsCommandHandler Handler => new(_repository, _readStore, _unitOfWork);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static UpdateSystemSettingsCommand Command(int samplingIntervalSeconds = 60, byte[]? expectedVersion = null) =>
        new(samplingIntervalSeconds, 60, 10080, 15, 60m, 3, 30, 30, expectedVersion);

    [Fact] // HU-02 · Regla: el administrador mantiene los parámetros de AppSetting
    public async Task Update_ValidValues_ReplacesAndSaves()
    {
        await Handler.HandleAsync(Command(expectedVersion: [1, 2, 3]), Token);

        await _repository.Received(1).LoadAsync(Arg.Any<CancellationToken>());
        _repository.Received(1).EnsureVersion(Arg.Is<byte[]>(v => v.SequenceEqual(new byte[] { 1, 2, 3 })));
        _repository.Received(1).Replace(Arg.Is<SystemSettings>(s => s.SamplingIntervalSeconds == 60));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _readStore.Received(1).GetAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Regla D-08: valores inválidos se rechazan sin tocar la base
    public async Task Update_InvalidValues_ThrowsBeforeLoading()
    {
        var update = () => Handler.HandleAsync(Command(samplingIntervalSeconds: 70), Token);

        await update.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("El intervalo de muestreo debe dividir exactamente la duración base");
        await _repository.DidNotReceive().LoadAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Regla: concurrencia optimista con If-Match (otro administrador guardó antes)
    public async Task Update_StaleVersion_DoesNotSave()
    {
        _repository.When(r => r.EnsureVersion(Arg.Any<byte[]>())).Throw(new ConcurrencyConflictException("cambió"));

        var update = () => Handler.HandleAsync(Command(expectedVersion: [9]), Token);

        await update.Should().ThrowAsync<ConcurrencyConflictException>();
        _repository.DidNotReceive().Replace(Arg.Any<SystemSettings>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // HU-02 · Regla: sin If-Match no se comprueba la versión
    public async Task Update_WithoutIfMatch_SkipsVersionCheck()
    {
        await Handler.HandleAsync(Command(), Token);

        _repository.DidNotReceive().EnsureVersion(Arg.Any<byte[]>());
    }
}
