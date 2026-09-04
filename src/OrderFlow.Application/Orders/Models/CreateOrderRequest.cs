namespace OrderFlow.Application.Orders.Models;

public sealed record CreateOrderRequest(
    string ExternalOrderId,
    string CustomerNumber,
    string CustomerName,
    IReadOnlyList<CreateOrderItemRequest> Items);
