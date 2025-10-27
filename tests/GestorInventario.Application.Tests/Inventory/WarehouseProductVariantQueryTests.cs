using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Inventory.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Application.Warehouses.Queries;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Inventory;

public class WarehouseProductVariantQueryTests
{
    [Fact]
    public async Task GetWarehouseProductVariants_ShouldReturnAssignments()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(GetWarehouseProductVariants_ShouldReturnAssignments));
        var product = new Product
        {
            Code = "PRD-010",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 12,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-010",
            Attributes = "color=verde",
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Centro", TenantId = 1 };
        var assignment = new WarehouseProductVariant
        {
            Warehouse = warehouse,
            Variant = variant,
            MinimumQuantity = 4,
            TargetQuantity = 7,
            TenantId = 1
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.WarehouseProductVariants.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new GetWarehouseProductVariantsQueryHandler(context);
        var results = await handler.Handle(new GetWarehouseProductVariantsQuery(warehouse.Id), CancellationToken.None);

        results.Should().ContainSingle();
        var dto = results.Single();
        dto.MinimumQuantity.Should().Be(4);
        dto.VariantSku.Should().Be("SKU-010");
    }

    [Fact]
    public async Task GetWarehouseProductVariants_ShouldThrow_WhenWarehouseMissing()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(GetWarehouseProductVariants_ShouldThrow_WhenWarehouseMissing));
        var handler = new GetWarehouseProductVariantsQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetWarehouseProductVariantsQuery(99), CancellationToken.None));
    }

    [Fact]
    public async Task GetVariantWarehouseAssignments_ShouldReturnWarehouses()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(GetVariantWarehouseAssignments_ShouldReturnWarehouses));
        var product = new Product
        {
            Code = "PRD-011",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 30,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-011",
            Attributes = "size=S",
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Este", TenantId = 1 };
        var assignment = new WarehouseProductVariant
        {
            Warehouse = warehouse,
            Variant = variant,
            MinimumQuantity = 2,
            TargetQuantity = 5,
            TenantId = 1
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.WarehouseProductVariants.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new GetVariantWarehouseAssignmentsQueryHandler(context);
        var results = await handler.Handle(new GetVariantWarehouseAssignmentsQuery(variant.Id), CancellationToken.None);

        results.Should().ContainSingle();
        results.Single().WarehouseName.Should().Be("Este");
    }

    [Fact]
    public async Task GetVariantWarehouseAssignments_ShouldThrow_WhenVariantMissing()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(GetVariantWarehouseAssignments_ShouldThrow_WhenVariantMissing));
        var handler = new GetVariantWarehouseAssignmentsQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetVariantWarehouseAssignmentsQuery(123), CancellationToken.None));
    }
}
