namespace OrderFlow.Application.Exceptions;

public sealed class DuplicateExternalOrderException : Exception
{
    public DuplicateExternalOrderException(
        string? externalOrderId = null,
        Exception? innerException = null)
        : base(
            string.IsNullOrWhiteSpace(externalOrderId)
                ? "A unique order identifier already exists."
                : $"An order with external order id '{externalOrderId}' already exists.",
            innerException)
    {
    }
}
