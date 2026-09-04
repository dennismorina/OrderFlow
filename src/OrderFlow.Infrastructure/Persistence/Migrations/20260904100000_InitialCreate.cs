using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrderFlowDbContext))]
[Migration("20260904100000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Consumer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InboxMessages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ExternalOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CustomerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                RoutingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FulfillmentRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                CustomerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FulfillmentRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OrderItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderItems_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "OrderStatusHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FromStatus = table.Column<int>(type: "int", nullable: false),
                ToStatus = table.Column<int>(type: "int", nullable: false),
                ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderStatusHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderStatusHistory_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FulfillmentRecords_MessageId",
            table: "FulfillmentRecords",
            column: "MessageId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FulfillmentRecords_OrderId",
            table: "FulfillmentRecords",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_InboxMessages_MessageId_Consumer",
            table: "InboxMessages",
            columns: new[] { "MessageId", "Consumer" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_OrderId",
            table: "OrderItems",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_ExternalOrderId",
            table: "Orders",
            column: "ExternalOrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_OrderNumber",
            table: "Orders",
            column: "OrderNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderStatusHistory_OrderId_ChangedAtUtc",
            table: "OrderStatusHistory",
            columns: new[] { "OrderId", "ChangedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FulfillmentRecords");
        migrationBuilder.DropTable(name: "InboxMessages");
        migrationBuilder.DropTable(name: "OrderItems");
        migrationBuilder.DropTable(name: "OrderStatusHistory");
        migrationBuilder.DropTable(name: "OutboxMessages");
        migrationBuilder.DropTable(name: "Orders");
    }
}
