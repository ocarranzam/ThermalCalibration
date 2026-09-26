using Thermal.Application.Abstractions;
using Thermal.Domain.Settings;

namespace Thermal.Application.Settings;

/// <summary>Parámetros del sistema con su versión (<c>ETag</c>) y fecha de la última edición.</summary>
public sealed record SystemSettingsDto(
    int SamplingIntervalSeconds,
    int BaseSessionMinutes,
    int MaxSessionMinutes,
    int RestPeriodMinutes,
    decimal SensorLossThresholdPct,
    int SensorLossCriticalAfterSamples,
    int SensorLossFailMinutes,
    int AboveLimitCriticalMinutes,
    int MinValidSamples,
    DateTimeOffset UpdatedAt,
    string Version);

/// <summary>Carga y reemplaza los parámetros del sistema (tabla clave-valor <c>AppSetting</c>).</summary>
public interface ISystemSettingsRepository
{
    Task<SystemSettings> LoadAsync(CancellationToken cancellationToken);

    /// <exception cref="Exceptions.ConcurrencyConflictException">La versión ya no es la esperada.</exception>
    void EnsureVersion(byte[] expectedVersion);

    void Replace(SystemSettings settings);
}

public interface ISystemSettingsReadStore
{
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken);
}

public sealed record GetSystemSettingsQuery : IQuery<SystemSettingsDto>;

internal sealed class GetSystemSettingsQueryHandler(ISystemSettingsReadStore readStore)
    : IQueryHandler<GetSystemSettingsQuery, SystemSettingsDto>
{
    public Task<SystemSettingsDto> HandleAsync(GetSystemSettingsQuery query, CancellationToken cancellationToken) =>
        readStore.GetAsync(cancellationToken);
}

/// <summary>
/// Reemplaza todos los parámetros (PUT). Solo afectan a las sesiones que se inicien después: las demás conservan su
/// copia (RN-08). <see cref="ExpectedVersion"/> es el ETag de <c>If-Match</c> ya decodificado, o <c>null</c>.
/// </summary>
public sealed record UpdateSystemSettingsCommand(
    int SamplingIntervalSeconds,
    int BaseSessionMinutes,
    int MaxSessionMinutes,
    int RestPeriodMinutes,
    decimal SensorLossThresholdPct,
    int SensorLossCriticalAfterSamples,
    int SensorLossFailMinutes,
    int AboveLimitCriticalMinutes,
    byte[]? ExpectedVersion) : ICommand<SystemSettingsDto>;

internal sealed class UpdateSystemSettingsCommandHandler(
    ISystemSettingsRepository repository,
    ISystemSettingsReadStore readStore,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateSystemSettingsCommand, SystemSettingsDto>
{
    public async Task<SystemSettingsDto> HandleAsync(UpdateSystemSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = SystemSettings.Create(
            command.SamplingIntervalSeconds,
            command.BaseSessionMinutes,
            command.MaxSessionMinutes,
            command.RestPeriodMinutes,
            command.SensorLossThresholdPct,
            command.SensorLossCriticalAfterSamples,
            command.SensorLossFailMinutes,
            command.AboveLimitCriticalMinutes);

        await repository.LoadAsync(cancellationToken);
        if (command.ExpectedVersion is { } expected)
        {
            repository.EnsureVersion(expected);
        }

        repository.Replace(settings);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await readStore.GetAsync(cancellationToken);
    }
}
