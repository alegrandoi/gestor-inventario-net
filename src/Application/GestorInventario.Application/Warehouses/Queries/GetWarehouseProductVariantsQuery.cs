using System.Linq;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Warehouses.Queries;

public record GetWarehouseProductVariantsQuery(int WarehouseId) : IRequest<IReadOnlyCollection<WarehouseProductVariantAssignmentDto>>;

public class GetWarehouseProductVariantsQueryHandler : IRequestHandler<GetWarehouseProductVariantsQuery, IReadOnlyCollection<WarehouseProductVariantAssignmentDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetWarehouseProductVariantsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<WarehouseProductVariantAssignmentDto>> Handle(GetWarehouseProductVariantsQuery request, CancellationToken cancellationToken)
    {
        var warehouseExists = await context.Warehouses
            .AsNoTracking()
            .AnyAsync(warehouse => warehouse.Id == request.WarehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (!warehouseExists)
        {
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);
        }

        var assignments = await context.WarehouseProductVariants
            .AsNoTracking()
            .Include(assignment => assignment.Variant)!
                .ThenInclude(variant => variant!.Product)
            .Where(assignment => assignment.WarehouseId == request.WarehouseId)
            .OrderBy(assignment => assignment.Variant != null ? assignment.Variant.Sku : string.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return assignments
            .Select(assignment => assignment.ToAssignmentDto())
            .ToList();
    }
}
