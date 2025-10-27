using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Shipments.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Shipments.Queries;

public record GetShipmentByIdQuery(int ShipmentId) : IRequest<ShipmentDto>;

public class GetShipmentByIdQueryHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetShipmentByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShipmentDto> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var shipment = await context.Shipments
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Lines)
                .ThenInclude(line => line.SalesOrderLine)
                    .ThenInclude(orderLine => orderLine!.Variant)
                        .ThenInclude(variant => variant!.Product)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        }

        return shipment.ToDto();
    }
}
