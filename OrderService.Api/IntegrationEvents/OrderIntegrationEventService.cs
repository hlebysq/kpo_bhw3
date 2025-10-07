using MassTransit;
using Shared.Contracts.Events;

namespace OrderService.Api.IntegrationEvents;

public class OrderIntegrationEventService(IPublishEndpoint publishEndpoint, ILogger<OrderIntegrationEventService> logger) : IOrderIntegrationEventService
{
    public async Task PublishOrderCreatedEventAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Публикация события OrderCreatedIntegrationEvent для заказа {OrderId}", @event.OrderId);
        await publishEndpoint.Publish(@event, cancellationToken);
        logger.LogInformation("Событие для заказа {OrderId} успешно опубликовано", @event.OrderId);
    }
}
