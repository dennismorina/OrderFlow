namespace OrderFlow.Application.Orders.Models;

public sealed record CreateOrderItemRequest(
    string ProductNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice);
