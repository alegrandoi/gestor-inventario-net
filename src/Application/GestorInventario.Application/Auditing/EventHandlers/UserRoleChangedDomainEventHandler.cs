using System.Collections.Generic;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Auditing;
using MediatR;

namespace GestorInventario.Application.Auditing.EventHandlers;

public class UserRoleChangedDomainEventHandler : INotificationHandler<UserRoleChangedDomainEvent>
{
    private readonly IAuditTrail auditTrail;

    public UserRoleChangedDomainEventHandler(IAuditTrail auditTrail)
    {
        this.auditTrail = auditTrail;
    }

    public async Task Handle(UserRoleChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var changes = new Dictionary<string, AuditChange>
        {
            ["role"] = AuditChange.Updated(notification.PreviousRole, notification.NewRole)
        };

        var entry = new AuditTrailEntry(
            EntityName: "User",
            EntityId: notification.UserId,
            Action: "UserRoleChanged",
            Changes: changes,
            Description: $"Cambio de rol para {notification.Username}");

        await auditTrail.PersistAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
