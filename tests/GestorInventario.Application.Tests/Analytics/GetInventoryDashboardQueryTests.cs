using FluentAssertions;
using GestorInventario.Application.Analytics.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Infrastructure.Caching;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorInventario.Application.Tests.Analytics;

public class GetInventoryDashboardQueryTests
{
    [Fact]
    public async Task Handle_ShouldAggregateMetricsAcrossEntities()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldAggregateMetricsAcrossEntities));

        var product = new Product
        {
            Code = "SKU-100",
            Name = "Zapatilla técnica",
            DefaultPrice = 80,
            Currency = "EUR",
            IsActive = true
        };

        var variant = new ProductVariant
        {
            Product = product,
            Sku = "SKU-100-BLUE-42",
            Attributes = "color=azul;talla=42",
            Price = 90
        };

        var warehouse = new Warehouse
        {
            Name = "Central",
            Description = "Almacén principal"
        };

        var stock = new InventoryStock
        {
            Variant = variant,
            Warehouse = warehouse,
            Quantity = 5,
            ReservedQuantity = 1,
            MinStockLevel = 8
        };

        var customer = new Customer
        {
            Name = "Cliente corporativo",
            Email = "cliente@example.com"
        };

        var salesOrder = new SalesOrder
        {
            Customer = customer,
            OrderDate = DateTime.UtcNow.AddDays(-5),
            Status = SalesOrderStatus.Delivered,
            TotalAmount = 180,
            Currency = "EUR"
        };

        var salesOrderLine = new SalesOrderLine
        {
            SalesOrder = salesOrder,
            Variant = variant,
            Quantity = 2,
            UnitPrice = 90,
            TotalLine = 180
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        context.Customers.Add(customer);
        context.SalesOrders.Add(salesOrder);
        context.SalesOrderLines.Add(salesOrderLine);

        await context.SaveChangesAsync();

        var cache = new InMemoryDistributedCache();
        var keyRegistry = new DistributedCacheKeyRegistry(cache);
        var handler = new GetInventoryDashboardQueryHandler(
            context,
            cache,
            keyRegistry,
            NullLogger<GetInventoryDashboardQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetInventoryDashboardQuery(), CancellationToken.None);

        // Assert
        result.TotalProducts.Should().Be(1);
        result.ActiveProducts.Should().Be(1);
        result.LowStockVariants.Should().Be(1);
        result.TotalInventoryValue.Should().BeApproximately(360, 0.001m); // (5 - 1) * 90
        result.ReorderAlerts.Should().ContainSingle();
        result.TopSellingProducts.Should().ContainSingle(productDto => productDto.ProductId == product.Id);
        result.MonthlySales.Should().HaveCount(1);
    }
}
