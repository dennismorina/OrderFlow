using OrderFlow.Domain.Orders;

namespace OrderFlow.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
