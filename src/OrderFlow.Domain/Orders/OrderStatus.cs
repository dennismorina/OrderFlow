namespace OrderFlow.Domain.Orders;

public enum OrderStatus
{
    Created = 0,
    Validated = 1,
    Approved = 2,
    Processing = 3,
    Completed = 4,
    Cancelled = 5
}
