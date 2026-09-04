using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OrderFlow.Infrastructure.Messaging;

public sealed class RabbitMqPublisher
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(
        string routingKey,
        string payload,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true
        };

        await using var connection =
            await factory.CreateConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqTopology.ExchangeName,
            type: RabbitMqTopology.ExchangeTypeName,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: RabbitMqTopology.FulfillmentQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: RabbitMqTopology.FulfillmentQueueName,
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: RabbitMqTopology.OrderApprovedRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true,
            MessageId = messageId.ToString()
        };

        await channel.BasicPublishAsync(
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Published and confirmed message {MessageId} with routing key {RoutingKey}.",
            messageId,
            routingKey);
    }
}
