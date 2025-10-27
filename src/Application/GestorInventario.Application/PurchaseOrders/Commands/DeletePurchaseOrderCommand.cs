using GestorInventario.Application.Common.Exceptions;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;

namespace GestorInventario.Application.PurchaseOrders.Commands;

public record DeletePurchaseOrderCommand(int OrderId) : IRequest;

public class DeletePurchaseOrderCommandHandler : IRequestHandler<DeletePurchaseOrderCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeletePurchaseOrderCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders.FindAsync([request.OrderId], cancellationToken).ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(PurchaseOrder), request.OrderId);
        }

        if (order.Status is PurchaseOrderStatus.Received)
        {
            throw new ApplicationValidationException("Received purchase orders cannot be deleted.");
        }

        context.PurchaseOrders.Remove(order);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
