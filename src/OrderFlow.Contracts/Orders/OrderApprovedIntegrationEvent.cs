namespace OrderFlow.Contracts.Orders;

public sealed record OrderApprovedIntegrationEvent(
    Guid MessageId,
    Guid OrderId,
    string OrderNumber,
    string CustomerNumber,
    DateTime OccurredAtUtc);
