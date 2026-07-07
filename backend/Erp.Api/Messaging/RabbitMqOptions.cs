namespace Erp.Api.Messaging;

public sealed class RabbitMqOptions
{
    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "erp.events";
    public string LowStockQueueName { get; set; } = "erp.low-stock-alerts";
    public int RetryDelaySeconds { get; set; } = 10;
}
