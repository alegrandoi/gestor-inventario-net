using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.AuditLogs.Commands;

public sealed record DeleteAuditLogCommand(int Id) : IRequest;

public sealed class DeleteAuditLogCommandHandler : IRequestHandler<DeleteAuditLogCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteAuditLogCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteAuditLogCommand request, CancellationToken cancellationToken)
    {
        var auditLog = await context.AuditLogs
            .FindAsync([request.Id], cancellationToken)
            .ConfigureAwait(false);

        if (auditLog is null)
        {
            throw new NotFoundException(nameof(AuditLog), request.Id);
        }

        context.AuditLogs.Remove(auditLog);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
