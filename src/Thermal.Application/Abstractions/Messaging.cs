namespace Thermal.Application.Abstractions;

// CQRS lógico con interfaces propias en lugar de MediatR (ADR-001 §2.2): el contenedor de
// dependencias resuelve el handler de cada comando o consulta.

/// <summary>Comando que cambia el estado y devuelve <typeparamref name="TResult"/>.</summary>
public interface ICommand<TResult>;

/// <summary>Consulta de solo lectura que devuelve <typeparamref name="TResult"/>.</summary>
public interface IQuery<TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
