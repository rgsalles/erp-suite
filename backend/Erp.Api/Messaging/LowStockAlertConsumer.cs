using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Erp.Api.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Erp.Api.Messaging;

public sealed class LowStockAlertConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<LowStockAlertConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("RabbitMQ is disabled. Low stock consumer will not start.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ consumer unavailable. Retrying in {Seconds} seconds.", _options.RetryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(_options.RetryDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task StartConsumerAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var consumerStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(_options.LowStockQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.LowStockQueueName, _options.ExchangeName, RabbitMqRoutingKeys.StockMovementCreated);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<StockMovementCreatedIntegrationEvent>(json, JsonOptions);

                if (message is not null)
                {
                    await HandleStockMovementAsync(message, stoppingToken);
                }

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing RabbitMQ low stock event.");
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        void CompleteConsumer(ShutdownEventArgs eventArgs)
        {
            if (stoppingToken.IsCancellationRequested || eventArgs.Initiator == ShutdownInitiator.Application)
            {
                consumerStopped.TrySetCanceled(stoppingToken);
                return;
            }

            consumerStopped.TrySetException(new BrokerUnreachableException(
                new InvalidOperationException($"RabbitMQ consumer stopped: {eventArgs.ReplyText}")));
        }

        connection.ConnectionShutdown += (_, eventArgs) => CompleteConsumer(eventArgs);
        channel.ModelShutdown += (_, eventArgs) => CompleteConsumer(eventArgs);

        channel.BasicConsume(_options.LowStockQueueName, autoAck: false, consumer);
        logger.LogInformation("RabbitMQ low stock consumer started on queue {QueueName}.", _options.LowStockQueueName);

        await consumerStopped.Task.WaitAsync(stoppingToken);
    }

    private async Task HandleStockMovementAsync(StockMovementCreatedIntegrationEvent message, CancellationToken cancellationToken)
    {
        if (message.CurrentStock >= message.MinimumStock)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();

        await auditLogService.LogAsync(new AuditEntry(
            UserId: message.UserId,
            UserName: "RabbitMQ Consumer",
            UserEmail: null,
            Action: "RabbitMq.LowStockDetected",
            HttpMethod: "EVENT",
            Path: RabbitMqRoutingKeys.StockMovementCreated,
            Controller: "RabbitMq",
            EntityName: "Material",
            EntityId: message.MaterialId.ToString(),
            StatusCode: StatusCodes.Status200OK,
            IpAddress: null,
            UserAgent: "RabbitMQ",
            Details: $"Material {message.MaterialCode} ficou abaixo do estoque minimo. Atual: {message.CurrentStock}; Minimo: {message.MinimumStock}."),
            cancellationToken);

        logger.LogWarning(
            "Low stock detected for material {MaterialCode}. Current: {CurrentStock}. Minimum: {MinimumStock}.",
            message.MaterialCode,
            message.CurrentStock,
            message.MinimumStock);
    }
}
