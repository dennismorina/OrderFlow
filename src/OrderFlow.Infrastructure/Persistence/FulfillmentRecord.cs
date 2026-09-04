namespace OrderFlow.Infrastructure.Persistence;

public sealed class FulfillmentRecord
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
}
