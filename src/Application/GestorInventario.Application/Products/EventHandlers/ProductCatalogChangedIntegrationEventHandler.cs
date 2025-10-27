using GestorInventario.Application.Common.Interfaces.Messaging;
using GestorInventario.Application.Products.Events;
using GestorInventario.Application.Products.IntegrationEvents;
using MediatR;

namespace GestorInventario.Application.Products.EventHandlers;

public class ProductCatalogChangedIntegrationEventHandler : INotificationHandler<ProductCatalogChangedDomainEvent>
{
    private readonly IIntegrationEventPublisher publisher;

    public ProductCatalogChangedIntegrationEventHandler(IIntegrationEventPublisher publisher)
    {
        this.publisher = publisher;
    }

    public Task Handle(ProductCatalogChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductCatalogChangedIntegrationEvent(
            notification.ProductId,
            notification.Code,
            notification.Name,
            notification.DefaultPrice,
            notification.Currency,
            notification.IsActive,
            notification.ChangeType);

        return publisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
