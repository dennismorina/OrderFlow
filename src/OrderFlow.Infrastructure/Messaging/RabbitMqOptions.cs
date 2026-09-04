namespace OrderFlow.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5673;
    public string UserName { get; set; } = "orderflow";
    public string Password { get; set; } = "orderflow_dev";
    public string VirtualHost { get; set; } = "/";
}
