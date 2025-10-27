using FluentAssertions;
using GestorInventario.Application.Inventory.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Inventory;

public class GetReplenishmentPlanQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldGenerateSuggestions_WhenStockIsBelowReorderPoint()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldGenerateSuggestions_WhenStockIsBelowReorderPoint));

        var product = new Product
        {
            Code = "PROD-RP",
            Name = "Producto Reposición",
            Currency = "EUR",
            DefaultPrice = 20m,
            SafetyStock = 5,
            LeadTimeDays = 7,
            ReorderPoint = 12,
            ReorderQuantity = 25
        };

        var variant = new ProductVariant
        {
            Product = product,
            Sku = "SKU-RP",
            Attributes = "color=azul"
        };

        var warehouse = new Warehouse { Name = "Central" };

        context.DemandHistory.AddRange(
            new DemandHistory
            {
                Variant = variant,
                Date = DateTime.UtcNow.AddDays(-5),
                Quantity = 8
            },
            new DemandHistory
            {
                Variant = variant,
                Date = DateTime.UtcNow.AddDays(-4),
                Quantity = 6
            });

        context.InventoryStocks.Add(new InventoryStock
        {
            Variant = variant,
            Warehouse = warehouse,
            Quantity = 10,
            ReservedQuantity = 2,
            MinStockLevel = 6
        });

        await context.SaveChangesAsync();

        var handler = new GetReplenishmentPlanQueryHandler(context);
        var result = await handler.Handle(new GetReplenishmentPlanQuery(DateTime.UtcNow.AddDays(-30), 30), CancellationToken.None);

        result.Suggestions.Should().ContainSingle();

        var suggestion = result.Suggestions.Single();
        suggestion.VariantSku.Should().Be("SKU-RP");
        suggestion.WarehouseName.Should().Be("Central");
        suggestion.RecommendedQuantity.Should().BeGreaterThan(0);
        suggestion.OnHand.Should().Be(8);
        suggestion.LeadTimeDemand.Should().BeGreaterThan(0);
    }
}
