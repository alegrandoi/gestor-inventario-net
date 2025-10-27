using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.SalesOrders.Queries;

public record GetSalesOrderByIdQuery(int Id) : IRequest<SalesOrderDto>;

public class GetSalesOrderByIdQueryHandler : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetSalesOrderByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<SalesOrderDto> Handle(GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await context.SalesOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Carrier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(o => o.Shipments)
                .ThenInclude(shipment => shipment.Warehouse)
            .Include(o => o.Shipments)
                .ThenInclude(shipment => shipment.Carrier)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(SalesOrder), request.Id);
        }

        return order.ToDto();
    }
}
