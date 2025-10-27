using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Warehouses.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Warehouses.Queries;

public record GetWarehousesQuery : IRequest<IReadOnlyCollection<WarehouseDto>>;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyCollection<WarehouseDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetWarehousesQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await context.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.WarehouseProductVariants)
                .ThenInclude(assignment => assignment.Variant)
                    .ThenInclude(variant => variant!.Product)
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return warehouses
            .Select(warehouse => warehouse.ToDto())
            .ToList();
    }
}
