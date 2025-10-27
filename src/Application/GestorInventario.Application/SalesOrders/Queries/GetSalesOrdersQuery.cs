using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.SalesOrders.Queries;

public record GetSalesOrdersQuery(int? CustomerId, SalesOrderStatus? Status) : IRequest<IReadOnlyCollection<SalesOrderDto>>;

public class GetSalesOrdersQueryHandler : IRequestHandler<GetSalesOrdersQuery, IReadOnlyCollection<SalesOrderDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetSalesOrdersQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<SalesOrderDto>> Handle(GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = context.SalesOrders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Carrier)
            .Include(order => order.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .Include(order => order.Lines)
                .ThenInclude(line => line.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(order => order.Shipments)
                .ThenInclude(shipment => shipment.Warehouse)
            .Include(order => order.Shipments)
                .ThenInclude(shipment => shipment.Carrier)
            .AsQueryable();

        if (request.CustomerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == request.CustomerId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(order => order.Status == request.Status.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return orders
            .Select(order => order.ToDto())
            .ToList();
    }
}
