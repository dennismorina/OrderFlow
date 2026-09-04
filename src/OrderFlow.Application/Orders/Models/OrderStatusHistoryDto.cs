using OrderFlow.Domain.Orders;

namespace OrderFlow.Application.Orders.Models;

public sealed record OrderStatusHistoryDto(
    Guid Id,
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    DateTime ChangedAtUtc,
    string? Reason);
