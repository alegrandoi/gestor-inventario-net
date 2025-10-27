using GestorInventario.Application.Common.Interfaces.Messaging;
using GestorInventario.Application.SalesOrders.Events;
using GestorInventario.Application.SalesOrders.IntegrationEvents;
using MediatR;

namespace GestorInventario.Application.SalesOrders.EventHandlers;

public class SalesOrderIntegrationEventHandler :
    INotificationHandler<SalesOrderCreatedDomainEvent>,
    INotificationHandler<SalesOrderStatusChangedDomainEvent>
{
    private readonly IIntegrationEventPublisher publisher;

    public SalesOrderIntegrationEventHandler(IIntegrationEventPublisher publisher)
    {
        this.publisher = publisher;
    }

    public Task Handle(SalesOrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new SalesOrderChangedIntegrationEvent(
            notification.OrderId,
            notification.Status,
            notification.CustomerName,
            notification.TotalAmount,
            "created",
            notification.OrderDate);

        return publisher.PublishAsync(integrationEvent, cancellationToken);
    }

    public Task Handle(SalesOrderStatusChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new SalesOrderChangedIntegrationEvent(
            notification.OrderId,
            notification.NewStatus,
            notification.CustomerName,
            notification.TotalAmount,
            $"status:{notification.PreviousStatus}->{notification.NewStatus}",
            notification.UpdatedAt);

        return publisher.PublishAsync(integrationEvent, cancellationToken);
    }
}
