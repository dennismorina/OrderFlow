using OrderFlow.Domain.Orders;

namespace OrderFlow.Application.Orders.Models;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string ExternalOrderId,
    string CustomerNumber,
    string CustomerName,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? Version,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> History);
