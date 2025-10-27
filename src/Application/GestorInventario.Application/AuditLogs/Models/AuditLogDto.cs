using System;

namespace GestorInventario.Application.AuditLogs.Models;

public sealed record AuditLogDto(
    int Id,
    string EntityName,
    int? EntityId,
    string Action,
    string? Changes,
    int? UserId,
    string? Username,
    DateTime CreatedAt);
