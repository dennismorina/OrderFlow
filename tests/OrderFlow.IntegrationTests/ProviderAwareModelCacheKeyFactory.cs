using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace OrderFlow.IntegrationTests;

public sealed class ProviderAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => (
            context.GetType(),
            context.Database.ProviderName,
            designTime
        );

#pragma warning disable CS0618
    public object Create(DbContext context)
        => Create(context, false);
#pragma warning restore CS0618
}
