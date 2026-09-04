using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Orders;

namespace OrderFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<OrderService>();
        return services;
    }
}
