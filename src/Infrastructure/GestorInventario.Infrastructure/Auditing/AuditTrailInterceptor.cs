using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GestorInventario.Application.Common.Auditing;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.Auditing;

public class AuditTrailInterceptor : IAuditTrail
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IGestorInventarioDbContext context;
    private readonly ICurrentUserService currentUserService;
    private readonly ILogger<AuditTrailInterceptor> logger;

    public AuditTrailInterceptor(
        IGestorInventarioDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AuditTrailInterceptor> logger)
    {
        this.context = context;
        this.currentUserService = currentUserService;
        this.logger = logger;
    }

    public async Task PersistAsync(AuditTrailEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var payload = SerializePayload(entry);

            var auditLog = new AuditLog
            {
                EntityName = entry.EntityName,
                EntityId = entry.EntityId,
                Action = entry.Action,
                Changes = payload,
                UserId = currentUserService.UserId
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al persistir la auditoría para {EntityName} con acción {Action}",
                entry.EntityName,
                entry.Action);
            throw;
        }
    }

    private string SerializePayload(AuditTrailEntry entry)
    {
        var payload = new
        {
            description = entry.Description,
            changes = entry.Changes,
            metadata = new
            {
                performedBy = currentUserService.UserName,
                performedById = currentUserService.UserId,
                timestamp = DateTime.UtcNow
            }
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }
}
