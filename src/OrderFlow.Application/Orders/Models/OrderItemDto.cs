namespace OrderFlow.Application.Orders.Models;

public sealed record OrderItemDto(
    Guid Id,
    string ProductNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice);
