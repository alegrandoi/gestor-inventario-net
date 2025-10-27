using FluentAssertions;
using GestorInventario.Application.Inventory.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Inventory;

public class GetInventoryOverviewQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRespectFiltersAndReturnStocks()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldRespectFiltersAndReturnStocks));

        var product = new Product
        {
            Code = "PROD-100",
            Name = "Producto Filtro",
            Currency = "EUR",
            DefaultPrice = 12m
        };

        var variantA = new ProductVariant
        {
            Product = product,
            Sku = "SKU-A",
            Attributes = "talla=M"
        };

        var variantB = new ProductVariant
        {
            Product = product,
            Sku = "SKU-B",
            Attributes = "talla=L"
        };

        var warehouseNorth = new Warehouse { Name = "Norte" };
        var warehouseSouth = new Warehouse { Name = "Sur" };

        context.InventoryStocks.AddRange(
            new InventoryStock
            {
                Variant = variantA,
                Warehouse = warehouseNorth,
                Quantity = 10,
                ReservedQuantity = 2,
                MinStockLevel = 5
            },
            new InventoryStock
            {
                Variant = variantB,
                Warehouse = warehouseSouth,
                Quantity = 3,
                ReservedQuantity = 1,
                MinStockLevel = 4
            });

        await context.SaveChangesAsync();

        var handler = new GetInventoryOverviewQueryHandler(context);

        var allStocks = await handler.Handle(new GetInventoryOverviewQuery(null, null, false), CancellationToken.None);
        allStocks.Should().HaveCount(2);

        var filteredByWarehouse = await handler.Handle(
            new GetInventoryOverviewQuery(warehouseNorth.Id, null, false),
            CancellationToken.None);

        filteredByWarehouse.Should().ContainSingle(stock => stock.WarehouseId == warehouseNorth.Id);

        var belowMinimum = await handler.Handle(new GetInventoryOverviewQuery(null, null, true), CancellationToken.None);
        belowMinimum.Should().ContainSingle(stock => stock.VariantSku == "SKU-B");
        belowMinimum.Should().AllSatisfy(stock => stock.Quantity.Should().BeLessOrEqualTo(stock.MinStockLevel));
    }
}
