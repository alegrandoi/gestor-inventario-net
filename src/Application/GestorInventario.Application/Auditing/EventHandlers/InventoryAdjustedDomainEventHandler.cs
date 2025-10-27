using System.Collections.Generic;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Auditing;
using MediatR;

namespace GestorInventario.Application.Auditing.EventHandlers;

public class InventoryAdjustedDomainEventHandler : INotificationHandler<InventoryAdjustedDomainEvent>
{
    private readonly IAuditTrail auditTrail;

    public InventoryAdjustedDomainEventHandler(IAuditTrail auditTrail)
    {
        this.auditTrail = auditTrail;
    }

    public async Task Handle(InventoryAdjustedDomainEvent notification, CancellationToken cancellationToken)
    {
        var changes = new Dictionary<string, AuditChange>();

        foreach (var adjustment in notification.Adjustments)
        {
            var key = $"warehouse:{adjustment.WarehouseId}";
            changes[key] = AuditChange.Updated(adjustment.QuantityBefore, adjustment.QuantityAfter);
        }

        changes["transactionType"] = AuditChange.Created(notification.TransactionType.ToString());
        changes["requestedQuantity"] = AuditChange.Created(notification.Quantity);

        if (notification.DestinationWarehouseId.HasValue)
        {
            changes["destinationWarehouseId"] = AuditChange.Created(notification.DestinationWarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(notification.ReferenceType) || notification.ReferenceId.HasValue)
        {
            changes["reference"] = AuditChange.Created(new
            {
                type = notification.ReferenceType,
                id = notification.ReferenceId
            });
        }

        if (!string.IsNullOrWhiteSpace(notification.Notes))
        {
            changes["notes"] = AuditChange.Created(notification.Notes);
        }

        var entry = new AuditTrailEntry(
            EntityName: "InventoryStock",
            EntityId: notification.VariantId,
            Action: "InventoryAdjusted",
            Changes: changes,
            Description: $"{notification.TransactionType} de inventario para SKU {notification.VariantSku} en {notification.ProductName}");

        await auditTrail.PersistAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
