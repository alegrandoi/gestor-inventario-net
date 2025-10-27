using System.Collections.Generic;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Auditing;
using MediatR;

namespace GestorInventario.Application.Auditing.EventHandlers;

public class ProductCreatedDomainEventHandler : INotificationHandler<ProductCreatedDomainEvent>
{
    private readonly IAuditTrail auditTrail;

    public ProductCreatedDomainEventHandler(IAuditTrail auditTrail)
    {
        this.auditTrail = auditTrail;
    }

    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var changes = new Dictionary<string, AuditChange>
        {
            ["code"] = AuditChange.Created(notification.Code),
            ["name"] = AuditChange.Created(notification.Name),
            ["defaultPrice"] = AuditChange.Created(notification.DefaultPrice),
            ["currency"] = AuditChange.Created(notification.Currency),
            ["isActive"] = AuditChange.Created(notification.IsActive)
        };

        var entry = new AuditTrailEntry(
            EntityName: "Product",
            EntityId: notification.ProductId,
            Action: "ProductCreated",
            Changes: changes,
            Description: $"Alta de producto {notification.Code}");

        await auditTrail.PersistAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
