using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Products.Commands;
using GestorInventario.Application.Products.Events;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestorInventario.Application.Tests.Products;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateProductWithVariantsAndImages()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldCreateProductWithVariantsAndImages));

        var publisher = new TestPublisher();
        var catalogEvents = new List<ProductCatalogChangedDomainEvent>();
        publisher.RegisterHandler<ProductCatalogChangedDomainEvent>((notification, _) =>
        {
            catalogEvents.Add(notification);
            return Task.CompletedTask;
        });

        context.Warehouses.AddRange(
            new Warehouse { Name = "Central" },
            new Warehouse { Name = "Secundario" });
        await context.SaveChangesAsync();

        var handler = new CreateProductCommandHandler(context, publisher);
        var command = new CreateProductCommand(
            Code: "SKU-001",
            Name: "Producto de prueba",
            Description: "Descripción",
            CategoryId: null,
            DefaultPrice: 25m,
            Currency: "EUR",
            TaxRateId: null,
            IsActive: true,
            WeightKg: 1.25m,
            HeightCm: 10m,
            WidthCm: 15m,
            LengthCm: 20m,
            LeadTimeDays: 7,
            SafetyStock: 5m,
            ReorderPoint: 12m,
            ReorderQuantity: 30m,
            RequiresSerialTracking: false,
            Variants: new[]
            {
                new CreateProductVariantRequest("SKU-001-A", "talla=M", 30m, null),
                new CreateProductVariantRequest("SKU-001-B", "talla=L", 32m, "1234567890")
            },
            Images: new[]
            {
                new CreateProductImageRequest("https://cdn.example.com/1.png", "Vista frontal"),
                new CreateProductImageRequest("https://cdn.example.com/2.png", null)
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Variants.Should().HaveCount(2);
        result.Images.Should().HaveCount(2);

        var product = await context.Products.FindAsync(result.Id);
        product.Should().NotBeNull();
        product!.Code.Should().Be("SKU-001");
        product.WeightKg.Should().Be(1.25m);
        product.SafetyStock.Should().Be(5m);
        product.ReorderPoint.Should().Be(12m);

        var stocks = await context.InventoryStocks
            .Include(stock => stock.Variant)
            .Where(stock => command.Variants.Select(variant => variant.Sku).Contains(stock.Variant!.Sku))
            .ToListAsync();
        stocks.Should().HaveCount(command.Variants.Count * 2);
        stocks.Should().OnlyContain(stock => stock.Quantity == 0 && stock.ReservedQuantity == 0 && stock.MinStockLevel == 12m);

        catalogEvents.Should().ContainSingle();
        catalogEvents[0].ChangeType.Should().Be(ProductCatalogChangeType.Created);
        catalogEvents[0].ProductId.Should().Be(result.Id);
    }
}
