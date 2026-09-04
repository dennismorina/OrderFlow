using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Startup;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 30;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<OrderFlowDbContext>();

                await dbContext.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        await using var finalScope = services.CreateAsyncScope();
        var finalDbContext = finalScope.ServiceProvider
            .GetRequiredService<OrderFlowDbContext>();

        await finalDbContext.Database.MigrateAsync(cancellationToken);
    }
}
