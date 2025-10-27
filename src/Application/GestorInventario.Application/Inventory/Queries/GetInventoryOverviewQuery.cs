using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Queries;

public record GetInventoryOverviewQuery(int? WarehouseId, int? VariantId, bool IncludeBelowMinimum) : IRequest<IReadOnlyCollection<InventoryStockDto>>;

public class GetInventoryOverviewQueryHandler : IRequestHandler<GetInventoryOverviewQuery, IReadOnlyCollection<InventoryStockDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetInventoryOverviewQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<InventoryStockDto>> Handle(GetInventoryOverviewQuery request, CancellationToken cancellationToken)
    {
        var query = context.InventoryStocks
            .AsNoTracking()
            .Include(stock => stock.Variant)
                .ThenInclude(variant => variant!.Product)
            .Include(stock => stock.Warehouse)
            .AsQueryable();

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(stock => stock.WarehouseId == request.WarehouseId.Value);
        }

        if (request.VariantId.HasValue)
        {
            query = query.Where(stock => stock.VariantId == request.VariantId.Value);
        }

        if (request.IncludeBelowMinimum)
        {
            query = query.Where(stock => stock.Quantity <= stock.MinStockLevel);
        }

        var stocks = await query
            .OrderBy(stock => stock.Variant != null ? stock.Variant.Sku : string.Empty)
            .ThenBy(stock => stock.Warehouse != null ? stock.Warehouse.Name : string.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return stocks
            .Select(stock => new InventoryStockDto(
                stock.Id,
                stock.VariantId,
                stock.WarehouseId,
                stock.Quantity,
                stock.ReservedQuantity,
                stock.MinStockLevel,
                stock.Variant?.Sku ?? string.Empty,
                stock.Variant?.Product?.Name ?? string.Empty,
                stock.Warehouse?.Name ?? string.Empty))
            .ToList();
    }
}
