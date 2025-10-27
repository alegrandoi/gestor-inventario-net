using System.Collections.Generic;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Auditing;
using MediatR;

namespace GestorInventario.Application.Auditing.EventHandlers;

public class ProductDeletedDomainEventHandler : INotificationHandler<ProductDeletedDomainEvent>
{
    private readonly IAuditTrail auditTrail;

    public ProductDeletedDomainEventHandler(IAuditTrail auditTrail)
    {
        this.auditTrail = auditTrail;
    }

    public async Task Handle(ProductDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var changes = new Dictionary<string, AuditChange>
        {
            ["code"] = AuditChange.Deleted(notification.Code),
            ["name"] = AuditChange.Deleted(notification.Name),
            ["defaultPrice"] = AuditChange.Deleted(notification.DefaultPrice),
            ["currency"] = AuditChange.Deleted(notification.Currency),
            ["isActive"] = AuditChange.Deleted(notification.WasActive)
        };

        var entry = new AuditTrailEntry(
            EntityName: "Product",
            EntityId: notification.ProductId,
            Action: "ProductDeleted",
            Changes: changes,
            Description: $"Baja de producto {notification.Code}");

        await auditTrail.PersistAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
