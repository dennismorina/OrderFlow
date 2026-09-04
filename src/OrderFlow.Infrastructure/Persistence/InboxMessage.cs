namespace OrderFlow.Infrastructure.Persistence;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
