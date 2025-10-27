using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PurchaseOrders.Queries;

public record GetPurchaseOrderByIdQuery(int Id) : IRequest<PurchaseOrderDto>;

public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetPurchaseOrderByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<PurchaseOrderDto> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await context.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            throw new NotFoundException(nameof(PurchaseOrder), request.Id);
        }

        return order.ToDto();
    }
}
