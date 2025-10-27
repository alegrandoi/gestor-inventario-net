using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PurchaseOrders.Queries;

public record GetPurchaseOrdersQuery(int? SupplierId, PurchaseOrderStatus? Status) : IRequest<IReadOnlyCollection<PurchaseOrderDto>>;

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, IReadOnlyCollection<PurchaseOrderDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetPurchaseOrdersQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = context.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Supplier)
            .Include(order => order.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .AsQueryable();

        if (request.SupplierId.HasValue)
        {
            query = query.Where(order => order.SupplierId == request.SupplierId.Value);
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
