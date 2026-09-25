namespace Thermal.Application.Abstractions;

/// <summary>Guarda los cambios de un comando en una sola transacción.</summary>
public interface IUnitOfWork
{
    /// <exception cref="Exceptions.ConflictException">Se violó una restricción de unicidad.</exception>
    /// <exception cref="Exceptions.ConcurrencyConflictException">El recurso cambió desde que se leyó.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
