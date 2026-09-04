using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests;

public sealed class OrderFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"orderflow-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:AutoMigrate"] = "false",
                    ["RabbitMq:Enabled"] = "false"
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderFlowDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<OrderFlowDbContext>>();

            services.AddDbContext<OrderFlowDbContext>(options =>
            {
                options
                    .UseInMemoryDatabase(_databaseName)
                    .ReplaceService<
                        IModelCacheKeyFactory,
                        ProviderAwareModelCacheKeyFactory>();
            });
        });
    }
}
