using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class OrderFlowDbContextFactory
    : IDesignTimeDbContextFactory<OrderFlowDbContext>
{
    public OrderFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ORDERFLOW_CONNECTION_STRING")
            ?? "Server=localhost,1436;Database=OrderFlow;User Id=sa;Password=OrderFlow_2026!;TrustServerCertificate=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<OrderFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new OrderFlowDbContext(options);
    }
}
