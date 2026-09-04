namespace OrderFlow.Application.Orders.Models;

public sealed record CreateOrderResult(
    OrderDto Order,
    bool IsIdempotentReplay);
