namespace Erp.Api.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(string routingKey, TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
