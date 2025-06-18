using Shared.Contracts.Events;

namespace OrderService.Api.IntegrationEvents;

public interface IOrderIntegrationEventService
{
    Task PublishOrderCreatedEventAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default);
}
