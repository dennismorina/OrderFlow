using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Orders;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderFlowDbContext _dbContext;

    public OrderRepository(OrderFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _dbContext.Orders
            .Include(order => order.Items)
            .Include(order => order.History)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<Order?> GetByExternalOrderIdAsync(
        string externalOrderId,
        CancellationToken cancellationToken = default)
        => _dbContext.Orders
            .Include(order => order.Items)
            .Include(order => order.History)
            .SingleOrDefaultAsync(
                order => order.ExternalOrderId == externalOrderId,
                cancellationToken);

    public async Task<IReadOnlyList<Order>> ListAsync(
        CancellationToken cancellationToken = default)
        => await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Include(order => order.History)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

    public Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
        => _dbContext.Orders.AddAsync(order, cancellationToken).AsTask();
}
