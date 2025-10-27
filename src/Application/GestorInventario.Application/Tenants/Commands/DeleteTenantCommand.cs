using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Tenants.Commands;

public record DeleteTenantCommand(int Id) : IRequest;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteTenantCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), request.Id);
        }

        context.Tenants.Remove(tenant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
