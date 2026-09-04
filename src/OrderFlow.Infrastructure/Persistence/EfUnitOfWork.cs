using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Exceptions;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly OrderFlowDbContext _dbContext;

    public EfUnitOfWork(OrderFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The order was changed by another process. Reload it and retry the operation.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateExternalOrderException(
                innerException: exception);
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
            return "<null>";

        if (value is byte[] bytes)
            return bytes.Length == 0
                ? "<empty byte[]>"
                : Convert.ToHexString(bytes);

        return value.ToString() ?? "<null>";
    }
}
