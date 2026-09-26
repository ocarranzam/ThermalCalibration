using Thermal.Application.Abstractions;

namespace Thermal.Application.ThermocoupleTypes;

/// <summary>Tipo de termopar con su rango físico de medición (se usa en la validación V10 de las lecturas).</summary>
public sealed record ThermocoupleTypeDto(string Code, string Name, decimal MinRangeC, decimal MaxRangeC);

/// <summary>Catálogo fijo de tipos de termopar (T, K), de solo lectura en la fase 1.</summary>
public interface IThermocoupleTypeReadStore
{
    Task<IReadOnlyList<ThermocoupleTypeDto>> ListAsync(CancellationToken cancellationToken);
}

public sealed record ListThermocoupleTypesQuery : IQuery<IReadOnlyList<ThermocoupleTypeDto>>;

internal sealed class ListThermocoupleTypesQueryHandler(IThermocoupleTypeReadStore readStore)
    : IQueryHandler<ListThermocoupleTypesQuery, IReadOnlyList<ThermocoupleTypeDto>>
{
    public Task<IReadOnlyList<ThermocoupleTypeDto>> HandleAsync(
        ListThermocoupleTypesQuery query, CancellationToken cancellationToken) =>
        readStore.ListAsync(cancellationToken);
}
