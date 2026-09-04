using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderFlow.Contracts.Orders;
using OrderFlow.Infrastructure.Messaging;
using OrderFlow.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderFlow.FulfillmentWorker;

public sealed class FulfillmentConsumer : BackgroundService
{
    private const string ConsumerName = "fulfillment";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<FulfillmentConsumer> _logger;

    public FulfillmentConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<FulfillmentConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "RabbitMQ consumer disconnected. Retrying.");

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
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

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

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

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var integrationEvent =
                    JsonSerializer.Deserialize<OrderApprovedIntegrationEvent>(
                        eventArgs.Body.Span);

                if (integrationEvent is null)
                {
                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken);

                    return;
                }

                await ProcessAsync(integrationEvent, cancellationToken);

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to process RabbitMQ delivery {DeliveryTag}.",
                    eventArgs.DeliveryTag);

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: RabbitMqTopology.FulfillmentQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Fulfillment consumer is listening on queue {Queue}.",
            RabbitMqTopology.FulfillmentQueueName);

        while (!cancellationToken.IsCancellationRequested &&
               connection.IsOpen &&
               channel.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private async Task ProcessAsync(
        OrderApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<OrderFlowDbContext>();

        var alreadyProcessed = await dbContext.InboxMessages
            .AnyAsync(
                message =>
                    message.MessageId == integrationEvent.MessageId &&
                    message.Consumer == ConsumerName,
                cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Message {MessageId} was already processed by {Consumer}.",
                integrationEvent.MessageId,
                ConsumerName);

            return;
        }

        dbContext.FulfillmentRecords.Add(new FulfillmentRecord
        {
            Id = Guid.NewGuid(),
            MessageId = integrationEvent.MessageId,
            OrderId = integrationEvent.OrderId,
            OrderNumber = integrationEvent.OrderNumber,
            CustomerNumber = integrationEvent.CustomerNumber,
            ReceivedAtUtc = DateTime.UtcNow
        });

        dbContext.InboxMessages.Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = integrationEvent.MessageId,
            Consumer = ConsumerName,
            ProcessedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} was handed to fulfillment.",
            integrationEvent.OrderNumber);
    }
}
