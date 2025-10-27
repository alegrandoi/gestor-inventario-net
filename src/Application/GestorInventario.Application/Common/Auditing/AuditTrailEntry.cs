using System.Collections.Generic;

namespace GestorInventario.Application.Common.Auditing;

public sealed record AuditTrailEntry(
    string EntityName,
    int? EntityId,
    string Action,
    IReadOnlyDictionary<string, AuditChange> Changes,
    string? Description = null);
