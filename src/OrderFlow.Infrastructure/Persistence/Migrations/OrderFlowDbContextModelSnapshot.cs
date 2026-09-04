using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace OrderFlow.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderFlowDbContext))]
partial class OrderFlowDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("OrderFlow.Domain.Orders.Order", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
            b.Property<string>("CustomerName").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.Property<string>("CustomerNumber").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<string>("ExternalOrderId").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("OrderNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<int>("Status").HasColumnType("int");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("datetime2");
            b.Property<byte[]>("Version")
                .IsRequired()
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");

            b.HasKey("Id");
            b.HasIndex("ExternalOrderId").IsUnique();
            b.HasIndex("OrderNumber").IsUnique();
            b.ToTable("Orders");
        });

        modelBuilder.Entity("OrderFlow.Domain.Orders.OrderItem", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<string>("Description").IsRequired().HasMaxLength(250).HasColumnType("nvarchar(250)");
            b.Property<Guid>("OrderId").HasColumnType("uniqueidentifier");
            b.Property<string>("ProductNumber").IsRequired().HasMaxLength(80).HasColumnType("nvarchar(80)");
            b.Property<decimal>("Quantity").HasPrecision(18, 3).HasColumnType("decimal(18,3)");
            b.Property<decimal>("UnitPrice").HasPrecision(18, 2).HasColumnType("decimal(18,2)");

            b.HasKey("Id");
            b.HasIndex("OrderId");
            b.ToTable("OrderItems");
        });

        modelBuilder.Entity("OrderFlow.Domain.Orders.OrderStatusHistory", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<DateTime>("ChangedAtUtc").HasColumnType("datetime2");
            b.Property<int>("FromStatus").HasColumnType("int");
            b.Property<Guid>("OrderId").HasColumnType("uniqueidentifier");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<int>("ToStatus").HasColumnType("int");

            b.HasKey("Id");
            b.HasIndex("OrderId", "ChangedAtUtc");
            b.ToTable("OrderStatusHistory");
        });

        modelBuilder.Entity("OrderFlow.Infrastructure.Persistence.OutboxMessage", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<int>("Attempts").HasColumnType("int");
            b.Property<string>("LastError").HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<DateTime>("OccurredAtUtc").HasColumnType("datetime2");
            b.Property<string>("Payload").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<DateTime?>("ProcessedAtUtc").HasColumnType("datetime2");
            b.Property<string>("RoutingKey").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("Type").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");

            b.HasKey("Id");
            b.HasIndex("ProcessedAtUtc", "OccurredAtUtc");
            b.ToTable("OutboxMessages");
        });

        modelBuilder.Entity("OrderFlow.Infrastructure.Persistence.InboxMessage", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<string>("Consumer").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<Guid>("MessageId").HasColumnType("uniqueidentifier");
            b.Property<DateTime>("ProcessedAtUtc").HasColumnType("datetime2");

            b.HasKey("Id");
            b.HasIndex("MessageId", "Consumer").IsUnique();
            b.ToTable("InboxMessages");
        });

        modelBuilder.Entity("OrderFlow.Infrastructure.Persistence.FulfillmentRecord", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<string>("CustomerNumber").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("MessageId").HasColumnType("uniqueidentifier");
            b.Property<Guid>("OrderId").HasColumnType("uniqueidentifier");
            b.Property<string>("OrderNumber").IsRequired().HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<DateTime>("ReceivedAtUtc").HasColumnType("datetime2");

            b.HasKey("Id");
            b.HasIndex("MessageId").IsUnique();
            b.HasIndex("OrderId");
            b.ToTable("FulfillmentRecords");
        });

        modelBuilder.Entity("OrderFlow.Domain.Orders.OrderItem", b =>
        {
            b.HasOne("OrderFlow.Domain.Orders.Order", null)
                .WithMany("Items")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("OrderFlow.Domain.Orders.OrderStatusHistory", b =>
        {
            b.HasOne("OrderFlow.Domain.Orders.Order", null)
                .WithMany("History")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("OrderFlow.Domain.Orders.Order", b =>
        {
            b.Navigation("History");
            b.Navigation("Items");
        });
    }
}
