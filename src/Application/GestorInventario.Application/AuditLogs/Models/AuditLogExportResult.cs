namespace GestorInventario.Application.AuditLogs.Models;

public sealed record AuditLogExportResult(
    string FileName,
    string ContentType,
    byte[] Content,
    string Signature,
    AuditLogExportFormat Format);
