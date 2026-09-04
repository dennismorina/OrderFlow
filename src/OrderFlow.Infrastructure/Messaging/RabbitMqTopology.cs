namespace OrderFlow.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string ExchangeName = "orderflow.events";
    public const string ExchangeTypeName = "topic";
    public const string OrderApprovedRoutingKey = "order.approved";
    public const string FulfillmentQueueName = "orderflow.fulfillment";
}
