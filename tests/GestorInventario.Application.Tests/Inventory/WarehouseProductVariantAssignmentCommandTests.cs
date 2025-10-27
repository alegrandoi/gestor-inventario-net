using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Inventory.Commands.WarehouseProductVariants;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Inventory;

public class WarehouseProductVariantAssignmentCommandTests
{
    [Fact]
    public async Task Create_ShouldPersistAssignment_WhenDataIsValid()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Create_ShouldPersistAssignment_WhenDataIsValid));
        var product = new Product
        {
            Code = "PRD-001",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 10,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-001",
            Attributes = "color=rojo",
            TenantId = 1
        };
        var warehouse = new Warehouse
        {
            Name = "Central",
            TenantId = 1
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new CreateWarehouseProductVariantAssignmentCommandHandler(context);
        var command = new CreateWarehouseProductVariantAssignmentCommand(warehouse.Id, variant.Id, 5, 8);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MinimumQuantity.Should().Be(5);
        result.TargetQuantity.Should().Be(8);
        result.VariantSku.Should().Be("SKU-001");
        result.WarehouseName.Should().Be("Central");

        var stored = await context.WarehouseProductVariants.FindAsync(result.Id);
        stored.Should().NotBeNull();
        stored!.MinimumQuantity.Should().Be(5);
        stored.TargetQuantity.Should().Be(8);
    }

    [Fact]
    public async Task Create_ShouldThrowValidation_WhenAssignmentExists()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Create_ShouldThrowValidation_WhenAssignmentExists));
        var product = new Product
        {
            Code = "PRD-002",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 15,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-002",
            Attributes = "size=M",
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Principal", TenantId = 1 };
        var existing = new WarehouseProductVariant
        {
            Warehouse = warehouse,
            Variant = variant,
            MinimumQuantity = 3,
            TargetQuantity = 5,
            TenantId = 1
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.WarehouseProductVariants.Add(existing);
        await context.SaveChangesAsync();

        var handler = new CreateWarehouseProductVariantAssignmentCommandHandler(context);
        var command = new CreateWarehouseProductVariantAssignmentCommand(warehouse.Id, variant.Id, 2, 4);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Update_ShouldModifyQuantities()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Update_ShouldModifyQuantities));
        var product = new Product
        {
            Code = "PRD-003",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 20,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-003",
            Attributes = "size=L",
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Sur", TenantId = 1 };
        var assignment = new WarehouseProductVariant
        {
            Warehouse = warehouse,
            Variant = variant,
            MinimumQuantity = 10,
            TargetQuantity = 12,
            TenantId = 1
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.WarehouseProductVariants.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new UpdateWarehouseProductVariantAssignmentCommandHandler(context);
        var command = new UpdateWarehouseProductVariantAssignmentCommand(assignment.Id, warehouse.Id, 15, 20);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MinimumQuantity.Should().Be(15);
        result.TargetQuantity.Should().Be(20);

        var stored = await context.WarehouseProductVariants.FindAsync(assignment.Id);
        stored.Should().NotBeNull();
        stored!.MinimumQuantity.Should().Be(15);
        stored.TargetQuantity.Should().Be(20);
    }

    [Fact]
    public async Task Delete_ShouldRemoveAssignment()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Delete_ShouldRemoveAssignment));
        var product = new Product
        {
            Code = "PRD-004",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 25,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-004",
            Attributes = "color=azul",
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Norte", TenantId = 1 };
        var assignment = new WarehouseProductVariant
        {
            Warehouse = warehouse,
            Variant = variant,
            MinimumQuantity = 6,
            TargetQuantity = 9,
            TenantId = 1
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.WarehouseProductVariants.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new DeleteWarehouseProductVariantAssignmentCommandHandler(context);
        var command = new DeleteWarehouseProductVariantAssignmentCommand(assignment.Id, warehouse.Id);

        await handler.Handle(command, CancellationToken.None);

        var remaining = await context.WarehouseProductVariants.FindAsync(assignment.Id);
        remaining.Should().BeNull();
    }
}
