using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Erp.Api.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync<TEvent>(string routingKey, TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent, JsonOptions));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = typeof(TEvent).Name;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            logger.LogInformation("Published RabbitMQ event {EventType} with routing key {RoutingKey}.", typeof(TEvent).Name, routingKey);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not publish RabbitMQ event {EventType}. Business transaction was kept committed.", typeof(TEvent).Name);
        }

        return Task.CompletedTask;
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };
    }
}
