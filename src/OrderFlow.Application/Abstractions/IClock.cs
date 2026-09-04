namespace OrderFlow.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
