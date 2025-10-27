using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Shipments.Models;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Shipments.Queries;

public record GetShipmentsQuery(
    int? SalesOrderId,
    ShipmentStatus? Status,
    DateTime? From,
    DateTime? To,
    int? WarehouseId) : IRequest<IReadOnlyCollection<ShipmentDto>>;

public class GetShipmentsQueryHandler : IRequestHandler<GetShipmentsQuery, IReadOnlyCollection<ShipmentDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetShipmentsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<ShipmentDto>> Handle(GetShipmentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Shipments
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Lines)
                .ThenInclude(line => line.SalesOrderLine)
                    .ThenInclude(orderLine => orderLine!.Variant)
                        .ThenInclude(variant => variant!.Product)
            .Include(s => s.Events)
            .AsQueryable();

        if (request.SalesOrderId.HasValue)
        {
            query = query.Where(shipment => shipment.SalesOrderId == request.SalesOrderId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(shipment => shipment.Status == request.Status.Value);
        }

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(shipment => shipment.WarehouseId == request.WarehouseId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(shipment => shipment.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(shipment => shipment.CreatedAt <= request.To.Value);
        }

        var shipments = await query
            .OrderByDescending(shipment => shipment.CreatedAt)
            .ThenByDescending(shipment => shipment.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return shipments.Select(shipment => shipment.ToDto()).ToList();
    }
}
