using GestorInventario.Domain.Entities;

namespace GestorInventario.Domain.Abstractions;

public abstract class TenantEntity : AuditableEntity, ITenantScopedEntity
{
    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
