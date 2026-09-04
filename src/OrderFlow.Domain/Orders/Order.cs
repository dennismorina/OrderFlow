using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders.Events;

namespace OrderFlow.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _history = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        string orderNumber,
        string externalOrderId,
        string customerNumber,
        string customerName,
        DateTime createdAtUtc)
    {
        Id = id;
        OrderNumber = orderNumber;
        ExternalOrderId = externalOrderId;
        CustomerNumber = customerNumber;
        CustomerName = customerName;
        Status = OrderStatus.Created;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string ExternalOrderId { get; private set; } = string.Empty;
    public string CustomerNumber { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> History => _history.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Order Create(
        string orderNumber,
        string externalOrderId,
        string customerNumber,
        string customerName,
        IEnumerable<OrderItem> items,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainRuleException("Order number is required.");
        }

        if (string.IsNullOrWhiteSpace(externalOrderId))
        {
            throw new DomainRuleException("External order id is required.");
        }

        if (string.IsNullOrWhiteSpace(customerNumber))
        {
            throw new DomainRuleException("Customer number is required.");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new DomainRuleException("Customer name is required.");
        }

        var orderItems = items.ToList();
        if (orderItems.Count == 0)
        {
            throw new DomainRuleException("An order must contain at least one item.");
        }

        var order = new Order(
            Guid.NewGuid(),
            orderNumber.Trim(),
            externalOrderId.Trim(),
            customerNumber.Trim(),
            customerName.Trim(),
            createdAtUtc);

        order._items.AddRange(orderItems);
        return order;
    }

    public void Validate(DateTime occurredAtUtc)
        => Transition(OrderStatus.Created, OrderStatus.Validated, occurredAtUtc);

    public void Approve(DateTime occurredAtUtc)
    {
        Transition(OrderStatus.Validated, OrderStatus.Approved, occurredAtUtc);

        _domainEvents.Add(new OrderApprovedDomainEvent(
            Guid.NewGuid(),
            Id,
            OrderNumber,
            CustomerNumber,
            occurredAtUtc));
    }

    public void StartProcessing(DateTime occurredAtUtc)
        => Transition(OrderStatus.Approved, OrderStatus.Processing, occurredAtUtc);

    public void Complete(DateTime occurredAtUtc)
        => Transition(OrderStatus.Processing, OrderStatus.Completed, occurredAtUtc);

    public void Cancel(DateTime occurredAtUtc, string? reason)
    {
        if (Status is OrderStatus.Processing or OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new DomainRuleException(
                $"Order in status '{Status}' cannot be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("A cancellation reason is required.");
        }

        var previous = Status;
        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = occurredAtUtc;
        _history.Add(OrderStatusHistory.Create(
            previous,
            OrderStatus.Cancelled,
            occurredAtUtc,
            reason));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Transition(
        OrderStatus requiredStatus,
        OrderStatus targetStatus,
        DateTime occurredAtUtc)
    {
        if (Status != requiredStatus)
        {
            throw new DomainRuleException(
                $"Order must be in status '{requiredStatus}' to transition to '{targetStatus}'. Current status is '{Status}'.");
        }

        Status = targetStatus;
        UpdatedAtUtc = occurredAtUtc;
        _history.Add(OrderStatusHistory.Create(
            requiredStatus,
            targetStatus,
            occurredAtUtc));
    }
}
