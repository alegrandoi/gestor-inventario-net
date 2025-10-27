using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using GestorInventario.Application.AuditLogs.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.AuditLogs.Queries;

public sealed record GenerateAuditLogExportQuery(
    AuditLogExportFormat Format,
    DateTime From,
    DateTime To,
    string RequestedBy,
    string Jurisdiction) : IRequest<AuditLogExportResult>;

public sealed class GenerateAuditLogExportQueryHandler : IRequestHandler<GenerateAuditLogExportQuery, AuditLogExportResult>
{
    private readonly IGestorInventarioDbContext context;

    public GenerateAuditLogExportQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<AuditLogExportResult> Handle(GenerateAuditLogExportQuery request, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            throw new ArgumentException("The 'from' date must be earlier than or equal to the 'to' date.");
        }

        var logs = await context.AuditLogs
            .Include(log => log.User)
            .Where(log => log.CreatedAt >= request.From && log.CreatedAt <= request.To)
            .OrderBy(log => log.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return request.Format switch
        {
            AuditLogExportFormat.Saft => BuildSaftExport(logs, request),
            AuditLogExportFormat.Sin => BuildSinExport(logs, request),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Format), request.Format, "Unsupported export format"),
        };
    }

    private static AuditLogExportResult BuildSaftExport(IReadOnlyCollection<Domain.Entities.AuditLog> logs, GenerateAuditLogExportQuery request)
    {
        var auditFile = new XElement("AuditFile",
            new XElement("Header",
                new XElement("AuditFileVersion", "1.0"),
                new XElement("CompanyID", request.Jurisdiction),
                new XElement("TaxRegistrationNumber", "N/A"),
                new XElement("CompanyName", "GestorInventario"),
                new XElement("BusinessName", "GestorInventario"),
                new XElement("CompanyAddress",
                    new XElement("AddressDetail", "Digital")),
                new XElement("FiscalYear", request.From.Year),
                new XElement("StartDate", request.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement("EndDate", request.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement("CurrencyCode", "EUR"),
                new XElement("DateCreated", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement("ProductID", "GestorInventario.Audit"),
                new XElement("ProductVersion", "1.0"),
                new XElement("SoftwareValidationNumber", "SelfCertified"),
                new XElement("HashControl", "SHA256"),
                new XElement("CertificationText", "Generated for SAF-T compliance")),
            new XElement("SourceDocuments",
                new XElement("AuditTrail",
                    logs.Select(log => new XElement("Entry",
                        new XElement("EntryNumber", log.Id),
                        new XElement("TransactionID", log.EntityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                        new XElement("JournalID", log.EntityName),
                        new XElement("Description", log.Action),
                        new XElement("PostingDate", log.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        new XElement("SystemEntryDate", log.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        new XElement("SourceID", log.UserId.HasValue ? log.UserId.Value.ToString(CultureInfo.InvariantCulture) : "system"),
                        new XElement("Hash", ComputeHash(log)))))));

        var content = Encoding.UTF8.GetBytes(auditFile.ToString(SaveOptions.DisableFormatting));
        var signature = ComputeSignature(content, request.RequestedBy, request.Jurisdiction);
        var fileName = $"audit-log-saft-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.xml";

        return new AuditLogExportResult(
            fileName,
            "application/xml",
            content,
            signature,
            AuditLogExportFormat.Saft);
    }

    private static AuditLogExportResult BuildSinExport(IReadOnlyCollection<Domain.Entities.AuditLog> logs, GenerateAuditLogExportQuery request)
    {
        var payload = new
        {
            jurisdiction = request.Jurisdiction,
            generatedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            range = new { from = request.From, to = request.To },
            entries = logs.Select(log => new
            {
                log.Id,
                log.EntityName,
                log.EntityId,
                log.Action,
                log.CreatedAt,
                user = log.User != null ? new { log.User.Id, log.User.Username } : null,
                checksum = ComputeHash(log),
            }),
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });

        var content = Encoding.UTF8.GetBytes(json);
        var signature = ComputeSignature(content, request.RequestedBy, request.Jurisdiction);
        var fileName = $"audit-log-sin-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.json";

        return new AuditLogExportResult(
            fileName,
            "application/json",
            content,
            signature,
            AuditLogExportFormat.Sin);
    }

    private static string ComputeSignature(byte[] content, string requestedBy, string jurisdiction)
    {
        using var sha256 = SHA256.Create();
        var scope = Encoding.UTF8.GetBytes($"{requestedBy}|{jurisdiction}");
        var buffer = new byte[content.Length + scope.Length];
        Buffer.BlockCopy(content, 0, buffer, 0, content.Length);
        Buffer.BlockCopy(scope, 0, buffer, content.Length, scope.Length);
        return Convert.ToBase64String(sha256.ComputeHash(buffer));
    }

    private static string ComputeHash(Domain.Entities.AuditLog log)
    {
        using var sha256 = SHA256.Create();
        var raw = Encoding.UTF8.GetBytes($"{log.Id}|{log.EntityName}|{log.EntityId}|{log.Action}|{log.CreatedAt:O}");
        return Convert.ToBase64String(sha256.ComputeHash(raw));
    }
}
