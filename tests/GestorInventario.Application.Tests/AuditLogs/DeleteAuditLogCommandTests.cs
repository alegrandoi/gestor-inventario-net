using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.AuditLogs.Commands;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.AuditLogs;

public sealed class DeleteAuditLogCommandTests
{
    [Fact]
    public async Task Handle_ShouldRemoveExistingAuditLog()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldRemoveExistingAuditLog));

        var auditLog = new AuditLog
        {
            EntityName = "Product",
            EntityId = 15,
            Action = "ProductDeleted"
        };

        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteAuditLogCommandHandler(context);

        await handler.Handle(new DeleteAuditLogCommand(auditLog.Id), CancellationToken.None);

        (await context.AuditLogs.FindAsync([auditLog.Id], CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenAuditLogIsMissing()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrowNotFound_WhenAuditLogIsMissing));

        var handler = new DeleteAuditLogCommandHandler(context);

        var action = async () => await handler.Handle(new DeleteAuditLogCommand(999), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }
}
