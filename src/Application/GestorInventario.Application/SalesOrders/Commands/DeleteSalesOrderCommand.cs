using GestorInventario.Application.Common.Exceptions;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;

namespace GestorInventario.Application.SalesOrders.Commands;

public record DeleteSalesOrderCommand(int OrderId) : IRequest;

public class DeleteSalesOrderCommandHandler : IRequestHandler<DeleteSalesOrderCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteSalesOrderCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders.FindAsync([request.OrderId], cancellationToken).ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(SalesOrder), request.OrderId);
        }

        if (order.Status is SalesOrderStatus.Shipped or SalesOrderStatus.Delivered)
        {
            throw new ApplicationValidationException("Shipped or delivered sales orders cannot be deleted.");
        }

        context.SalesOrders.Remove(order);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
