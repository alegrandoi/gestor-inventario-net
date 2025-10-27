using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Suppliers.Commands;

public record DeleteSupplierCommand(int Id) : IRequest;

public class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteSupplierCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (supplier is null)
        {
            throw new NotFoundException(nameof(Supplier), request.Id);
        }

        context.Suppliers.Remove(supplier);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
