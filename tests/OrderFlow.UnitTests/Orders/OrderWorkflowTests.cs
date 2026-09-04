using Xunit;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;

namespace OrderFlow.UnitTests.Orders;

public sealed class OrderWorkflowTests
{
    private static readonly DateTime Now =
        new(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_order_starts_in_created_status()
    {
        var order = CreateOrder();

        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Empty(order.History);
    }

    [Fact]
    public void Created_order_can_be_validated()
    {
        var order = CreateOrder();

        order.Validate(Now.AddMinutes(1));

        Assert.Equal(OrderStatus.Validated, order.Status);
        Assert.Single(order.History);
    }

    [Fact]
    public void Created_order_cannot_be_approved()
    {
        var order = CreateOrder();

        var exception = Assert.Throws<DomainRuleException>(
            () => order.Approve(Now.AddMinutes(1)));

        Assert.Contains("Validated", exception.Message);
    }

    [Fact]
    public void Validated_order_can_be_approved_and_emits_domain_event()
    {
        var order = CreateOrder();
        order.Validate(Now.AddMinutes(1));

        order.Approve(Now.AddMinutes(2));

        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.Contains(
            order.DomainEvents,
            domainEvent => domainEvent is OrderApprovedDomainEvent);
    }

    [Fact]
    public void Approved_order_can_start_processing()
    {
        var order = CreateOrder();
        order.Validate(Now.AddMinutes(1));
        order.Approve(Now.AddMinutes(2));

        order.StartProcessing(Now.AddMinutes(3));

        Assert.Equal(OrderStatus.Processing, order.Status);
    }

    [Fact]
    public void Processing_order_cannot_be_cancelled()
    {
        var order = CreateOrder();
        order.Validate(Now.AddMinutes(1));
        order.Approve(Now.AddMinutes(2));
        order.StartProcessing(Now.AddMinutes(3));

        Assert.Throws<DomainRuleException>(
            () => order.Cancel(Now.AddMinutes(4), "Customer request"));
    }

    [Fact]
    public void Approved_order_can_be_cancelled_with_reason()
    {
        var order = CreateOrder();
        order.Validate(Now.AddMinutes(1));
        order.Approve(Now.AddMinutes(2));

        order.Cancel(Now.AddMinutes(3), "Customer request");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Customer request", order.History.Last().Reason);
    }

    [Fact]
    public void Completed_order_is_final()
    {
        var order = CreateOrder();
        order.Validate(Now.AddMinutes(1));
        order.Approve(Now.AddMinutes(2));
        order.StartProcessing(Now.AddMinutes(3));
        order.Complete(Now.AddMinutes(4));

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Throws<DomainRuleException>(
            () => order.Cancel(Now.AddMinutes(5), "Too late"));
        Assert.Throws<DomainRuleException>(
            () => order.StartProcessing(Now.AddMinutes(5)));
    }

    private static Order CreateOrder()
        => Order.Create(
            "ORD-TEST-001",
            "SHOP-TEST-001",
            "C1000",
            "Test Customer",
            [
                OrderItem.Create(
                    "SKU-1",
                    "Test Item",
                    2,
                    12.50m)
            ],
            Now);
}
