using System.Text.Json.Serialization;

namespace GestorInventario.Application.Common.Auditing;

public sealed record AuditChange(
    [property: JsonPropertyName("old")] object? OldValue,
    [property: JsonPropertyName("new")] object? NewValue)
{
    public static AuditChange Created(object? newValue) => new(null, newValue);

    public static AuditChange Deleted(object? oldValue) => new(oldValue, null);

    public static AuditChange Updated(object? oldValue, object? newValue) => new(oldValue, newValue);
}
