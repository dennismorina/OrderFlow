using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Exceptions;
using OrderFlow.Application.Orders.Models;
using OrderFlow.Domain.Orders;

namespace OrderFlow.Application.Orders;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public OrderService(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CreateOrderResult> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var externalOrderId = request.ExternalOrderId?.Trim() ?? string.Empty;

        var existing = await _orders.GetByExternalOrderIdAsync(
            externalOrderId,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateOrderResult(Map(existing), true);
        }

        var now = _clock.UtcNow;
        var orderNumber = CreateOrderNumber(now);

        var items = request.Items?.Select(item => OrderItem.Create(
            item.ProductNumber,
            item.Description,
            item.Quantity,
            item.UnitPrice)) ?? [];

        var order = Order.Create(
            orderNumber,
            externalOrderId,
            request.CustomerNumber,
            request.CustomerName,
            items,
            now);

        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrderResult(Map(order), false);
    }

    public async Task<OrderDto> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredAsync(id, cancellationToken);
        return Map(order);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _orders.ListAsync(cancellationToken);
        return orders.Select(Map).ToList();
    }

    public Task<OrderDto> ValidateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => TransitionAsync(id, order => order.Validate(_clock.UtcNow), cancellationToken);

    public Task<OrderDto> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => TransitionAsync(id, order => order.Approve(_clock.UtcNow), cancellationToken);

    public Task<OrderDto> StartProcessingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => TransitionAsync(id, order => order.StartProcessing(_clock.UtcNow), cancellationToken);

    public Task<OrderDto> CompleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => TransitionAsync(id, order => order.Complete(_clock.UtcNow), cancellationToken);

    public Task<OrderDto> CancelAsync(
        Guid id,
        string reason,
        CancellationToken cancellationToken = default)
        => TransitionAsync(id, order => order.Cancel(_clock.UtcNow, reason), cancellationToken);

    private async Task<OrderDto> TransitionAsync(
        Guid id,
        Action<Order> transition,
        CancellationToken cancellationToken)
    {
        var order = await GetRequiredAsync(id, cancellationToken);
        transition(order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(order);
    }

    private async Task<Order> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await _orders.GetByIdAsync(id, cancellationToken)
           ?? throw new OrderNotFoundException(id);

    private static string CreateOrderNumber(DateTime now)
        => $"ORD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();

    private static OrderDto Map(Order order)
    {
        var items = order.Items
            .Select(item => new OrderItemDto(
                item.Id,
                item.ProductNumber,
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice))
            .ToList();

        var history = order.History
            .OrderBy(entry => entry.ChangedAtUtc)
            .Select(entry => new OrderStatusHistoryDto(
                entry.Id,
                entry.FromStatus,
                entry.ToStatus,
                entry.ChangedAtUtc,
                entry.Reason))
            .ToList();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.ExternalOrderId,
            order.CustomerNumber,
            order.CustomerName,
            order.Status,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.Version.Length == 0 ? null : Convert.ToBase64String(order.Version),
            items.Sum(item => item.TotalPrice),
            items,
            history);
    }
}
