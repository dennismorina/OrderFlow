using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Orders;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class OrderFlowDbContext : DbContext
{
    public OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<FulfillmentRecord> FulfillmentRecords => Set<FulfillmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.OrderNumber)
                .HasMaxLength(40)
                .IsRequired();

            builder.HasIndex(x => x.OrderNumber)
                .IsUnique();

            builder.Property(x => x.ExternalOrderId)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(x => x.ExternalOrderId)
                .IsUnique();

            builder.Property(x => x.CustomerNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.CustomerName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>();

            var versionProperty = builder.Property(x => x.Version);

            if (Database.IsSqlServer())
            {
                versionProperty.IsRowVersion();
            }
            else
            {
                versionProperty
                    .IsConcurrencyToken(false)
                    .ValueGeneratedNever();
            }

            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.History)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.History)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("OrderItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.ProductNumber)
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.Quantity)
                .HasPrecision(18, 3);

            builder.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            builder.Ignore(x => x.TotalPrice);
        });

        modelBuilder.Entity<OrderStatusHistory>(builder =>
        {
            builder.ToTable("OrderStatusHistory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.FromStatus)
                .HasConversion<int>();

            builder.Property(x => x.ToStatus)
                .HasConversion<int>();

            builder.Property(x => x.Reason)
                .HasMaxLength(500);

            builder.HasIndex(x => new { x.OrderId, x.ChangedAtUtc });
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.RoutingKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Payload)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            builder.Property(x => x.LastError)
                .HasMaxLength(2000);

            builder.HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc });
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("InboxMessages");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Consumer)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(x => new { x.MessageId, x.Consumer })
                .IsUnique();
        });

        modelBuilder.Entity<FulfillmentRecord>(builder =>
        {
            builder.ToTable("FulfillmentRecords");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderNumber)
                .HasMaxLength(40)
                .IsRequired();

            builder.Property(x => x.CustomerNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.MessageId)
                .IsUnique();

            builder.HasIndex(x => x.OrderId);
        });
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var ordersWithEvents = ChangeTracker
            .Entries<Order>()
            .Select(entry => entry.Entity)
            .Where(order => order.DomainEvents.Count > 0)
            .ToList();

        var outboxMessages = ordersWithEvents
            .SelectMany(order => order.DomainEvents)
            .Select(MapDomainEvent)
            .ToList();

        if (outboxMessages.Count > 0)
        {
            OutboxMessages.AddRange(outboxMessages);
        }

        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            foreach (var order in ordersWithEvents)
            {
                order.ClearDomainEvents();
            }

            return result;
        }
        catch
        {
            foreach (var message in outboxMessages)
            {
                Entry(message).State = EntityState.Detached;
            }

            throw;
        }
    }

    private static OutboxMessage MapDomainEvent(OrderFlow.Domain.Common.IDomainEvent domainEvent)
        => domainEvent switch
        {
            OrderApprovedDomainEvent approved => CreateOrderApprovedOutboxMessage(approved),
            _ => throw new InvalidOperationException(
                $"No outbox mapping exists for domain event '{domainEvent.GetType().Name}'.")
        };

    private static OutboxMessage CreateOrderApprovedOutboxMessage(
        OrderApprovedDomainEvent domainEvent)
    {
        var integrationEvent = new OrderApprovedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OrderId,
            domainEvent.OrderNumber,
            domainEvent.CustomerNumber,
            domainEvent.OccurredAtUtc);

        return new OutboxMessage
        {
            Id = domainEvent.EventId,
            Type = nameof(OrderApprovedIntegrationEvent),
            RoutingKey = Messaging.RabbitMqTopology.OrderApprovedRoutingKey,
            Payload = JsonSerializer.Serialize(integrationEvent),
            OccurredAtUtc = domainEvent.OccurredAtUtc
        };
    }
}
