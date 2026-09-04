namespace OrderFlow.Domain.Common;

public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string message)
        : base(message)
    {
    }
}
