using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Orders.Events;

public sealed record OrderApprovedDomainEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    string CustomerNumber,
    DateTime OccurredAtUtc) : IDomainEvent;
