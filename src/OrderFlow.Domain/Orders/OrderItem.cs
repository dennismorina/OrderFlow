using OrderFlow.Domain.Common;

namespace OrderFlow.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    private OrderItem(
        Guid id,
        string productNumber,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        Id = id;
        ProductNumber = productNumber;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductNumber { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    public static OrderItem Create(
        string productNumber,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productNumber))
        {
            throw new DomainRuleException("Product number is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainRuleException("Description is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleException("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainRuleException("Unit price must not be negative.");
        }

        return new OrderItem(
            Guid.NewGuid(),
            productNumber.Trim(),
            description.Trim(),
            quantity,
            unitPrice);
    }
}
