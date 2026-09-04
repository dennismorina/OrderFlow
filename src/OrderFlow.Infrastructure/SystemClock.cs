using OrderFlow.Application.Abstractions;

namespace OrderFlow.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
