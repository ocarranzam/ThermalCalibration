using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Thermal.Application.Abstractions;
using Thermal.Application.Exceptions;

namespace Thermal.Infrastructure.Persistence;

/// <summary>
/// Traduce los errores de la base a excepciones de la aplicación. La comprobación previa del handler
/// cubre el caso normal; esto cubre la carrera entre dos solicitudes simultáneas.
/// </summary>
internal sealed class UnitOfWork(ThermalDbContext context) : IUnitOfWork
{
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "El recurso fue modificado por otro usuario. Vuelva a leerlo antes de guardar.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: UniqueConstraintViolation or UniqueIndexViolation,
        })
        {
            throw new ConflictException("Ya existe un registro con esos datos únicos.", ex);
        }
    }
}
