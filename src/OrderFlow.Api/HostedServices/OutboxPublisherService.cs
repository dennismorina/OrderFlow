using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderFlow.Infrastructure.Messaging;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.HostedServices;

public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        RabbitMqPublisher publisher,
        IOptions<RabbitMqOptions> options,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("RabbitMQ publishing is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await PublishPendingAsync(stoppingToken);
                await Task.Delay(
                    processed == 0
                        ? TimeSpan.FromSeconds(1)
                        : TimeSpan.FromMilliseconds(100),
                    stoppingToken);
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
                    "Outbox publisher iteration failed.");

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<int> PublishPendingAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<OrderFlowDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _publisher.PublishAsync(
                    message.RoutingKey,
                    message.Payload,
                    message.Id,
                    cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.LastError = exception.Message.Length > 2000
                    ? exception.Message[..2000]
                    : exception.Message;

                _logger.LogWarning(
                    exception,
                    "Could not publish outbox message {MessageId}. Attempt {Attempt}.",
                    message.Id,
                    message.Attempts);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }
}
