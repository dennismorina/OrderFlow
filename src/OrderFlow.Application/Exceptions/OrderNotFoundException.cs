namespace OrderFlow.Application.Exceptions;

public sealed class OrderNotFoundException : Exception
{
    public OrderNotFoundException(Guid orderId)
        : base($"Order '{orderId}' was not found.")
    {
    }
}
