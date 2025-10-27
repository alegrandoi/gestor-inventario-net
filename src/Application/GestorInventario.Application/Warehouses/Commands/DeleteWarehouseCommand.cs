using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Warehouses.Commands;

public record DeleteWarehouseCommand(int Id) : IRequest;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteWarehouseCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (warehouse is null)
        {
            throw new NotFoundException(nameof(Warehouse), request.Id);
        }

        context.Warehouses.Remove(warehouse);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
