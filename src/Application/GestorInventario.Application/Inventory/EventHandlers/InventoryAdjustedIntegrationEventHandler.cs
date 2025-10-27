using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Interfaces.Messaging;
using GestorInventario.Application.Inventory.IntegrationEvents;
using MediatR;

namespace GestorInventario.Application.Inventory.EventHandlers;

public class InventoryAdjustedIntegrationEventHandler : INotificationHandler<InventoryAdjustedDomainEvent>
{
    private readonly IIntegrationEventPublisher publisher;

    public InventoryAdjustedIntegrationEventHandler(IIntegrationEventPublisher publisher)
    {
        this.publisher = publisher;
    }

    public Task Handle(InventoryAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new InventoryAdjustedIntegrationEvent(
            notification.VariantId,
            notification.VariantSku,
            notification.ProductName,
            notification.Adjustments,
            notification.TransactionType,
            notification.Quantity,
            notification.DestinationWarehouseId,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.Notes,
            notification.OccurredAt);

        return publisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
