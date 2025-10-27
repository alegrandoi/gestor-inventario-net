using System.Linq;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Queries;

public record GetVariantWarehouseAssignmentsQuery(int VariantId) : IRequest<IReadOnlyCollection<WarehouseProductVariantAssignmentDto>>;

public class GetVariantWarehouseAssignmentsQueryHandler : IRequestHandler<GetVariantWarehouseAssignmentsQuery, IReadOnlyCollection<WarehouseProductVariantAssignmentDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetVariantWarehouseAssignmentsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<WarehouseProductVariantAssignmentDto>> Handle(GetVariantWarehouseAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var variantExists = await context.ProductVariants
            .AsNoTracking()
            .AnyAsync(variant => variant.Id == request.VariantId, cancellationToken)
            .ConfigureAwait(false);

        if (!variantExists)
        {
            throw new NotFoundException(nameof(ProductVariant), request.VariantId);
        }

        var assignments = await context.WarehouseProductVariants
            .AsNoTracking()
            .Include(assignment => assignment.Warehouse)
            .Include(assignment => assignment.Variant)!
                .ThenInclude(variant => variant!.Product)
            .Where(assignment => assignment.VariantId == request.VariantId)
            .OrderBy(assignment => assignment.Warehouse != null ? assignment.Warehouse.Name : string.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return assignments
            .Select(assignment => assignment.ToAssignmentDto())
            .ToList();
    }
}
