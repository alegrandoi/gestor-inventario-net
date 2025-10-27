namespace GestorInventario.Domain.Abstractions;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; private set; }

    public void MarkCreated(DateTime timestampUtc)
    {
        CreatedAt = timestampUtc;
    }

    public void MarkUpdated(DateTime timestampUtc)
    {
        UpdatedAt = timestampUtc;
    }
}
