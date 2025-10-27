using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Queries;

public record GetReplenishmentPlanQuery(DateTime? FromDate, int PlanningWindowDays) : IRequest<ReplenishmentPlanDto>;

public class GetReplenishmentPlanQueryHandler : IRequestHandler<GetReplenishmentPlanQuery, ReplenishmentPlanDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetReplenishmentPlanQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ReplenishmentPlanDto> Handle(GetReplenishmentPlanQuery request, CancellationToken cancellationToken)
    {
        var windowDays = request.PlanningWindowDays > 0 ? request.PlanningWindowDays : 90;
        var windowStart = (request.FromDate ?? DateTime.UtcNow).AddDays(-windowDays);

        var demandGroups = await context.DemandHistory
            .Where(entry => entry.Date >= windowStart)
            .GroupBy(entry => entry.VariantId)
            .Select(group => new
            {
                VariantId = group.Key,
                TotalQuantity = group.Sum(entry => (double)entry.Quantity)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var demand = demandGroups.ToDictionary(
            group => group.VariantId,
            group => (decimal)group.TotalQuantity);

        var stocks = await context.InventoryStocks
            .Include(stock => stock.Variant)
                .ThenInclude(variant => variant!.Product)
            .Include(stock => stock.Warehouse)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var suggestions = new List<ReplenishmentSuggestionDto>();

        foreach (var stock in stocks)
        {
            if (stock.Variant is null || stock.Warehouse is null)
            {
                continue;
            }

            var product = stock.Variant.Product;
            if (product is null)
            {
                continue;
            }

            var totalQuantity = demand.TryGetValue(stock.VariantId, out var demandQuantity) ? demandQuantity : 0;
            var averageDailyDemand = windowDays > 0 ? Math.Round(totalQuantity / windowDays, 4) : 0;
            var safetyStock = product.SafetyStock ?? 0;
            var leadTimeDays = product.LeadTimeDays ?? 0;
            var leadTimeDemand = Math.Round(averageDailyDemand * leadTimeDays, 4);
            var reorderPoint = product.ReorderPoint ?? (leadTimeDemand + safetyStock);
            var onHand = stock.Quantity - stock.ReservedQuantity;

            if (onHand <= reorderPoint)
            {
                var targetQuantity = product.ReorderQuantity ?? (leadTimeDemand + safetyStock);
                var recommended = Math.Max(0, Math.Round(targetQuantity - onHand, 4));
                if (recommended > 0)
                {
                    suggestions.Add(new ReplenishmentSuggestionDto(
                        stock.VariantId,
                        stock.Variant.Sku,
                        product.Name,
                        stock.WarehouseId,
                        stock.Warehouse.Name,
                        onHand,
                        stock.ReservedQuantity,
                        recommended,
                        product.SafetyStock,
                        product.ReorderPoint,
                        leadTimeDemand,
                        averageDailyDemand));
                }
            }
        }

        return new ReplenishmentPlanDto(DateTime.UtcNow, suggestions);
    }
}
