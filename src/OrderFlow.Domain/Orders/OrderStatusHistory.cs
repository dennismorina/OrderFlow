namespace OrderFlow.Domain.Orders;

public sealed class OrderStatusHistory
{
    private OrderStatusHistory()
    {
    }

    private OrderStatusHistory(
        Guid id,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        DateTime changedAtUtc,
        string? reason)
    {
        Id = id;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAtUtc = changedAtUtc;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    internal static OrderStatusHistory Create(
        OrderStatus fromStatus,
        OrderStatus toStatus,
        DateTime changedAtUtc,
        string? reason = null)
        => new(Guid.NewGuid(), fromStatus, toStatus, changedAtUtc, reason);
}
