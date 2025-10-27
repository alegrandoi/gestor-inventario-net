using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Carriers.Commands;

public record DeleteCarrierCommand(int Id) : IRequest;

public class DeleteCarrierCommandHandler : IRequestHandler<DeleteCarrierCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteCarrierCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteCarrierCommand request, CancellationToken cancellationToken)
    {
        var carrier = await context.Carriers.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);
        if (carrier is null)
        {
            throw new NotFoundException(nameof(Carrier), request.Id);
        }

        var hasSalesOrders = await context.SalesOrders
            .AnyAsync(order => order.CarrierId == request.Id, cancellationToken)
            .ConfigureAwait(false);

        var hasShipments = !hasSalesOrders && await context.Shipments
            .AnyAsync(shipment => shipment.CarrierId == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (hasSalesOrders || hasShipments)
        {
            throw new ValidationException("El transportista está asociado a pedidos o envíos y no puede eliminarse.");
        }

        context.Carriers.Remove(carrier);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
