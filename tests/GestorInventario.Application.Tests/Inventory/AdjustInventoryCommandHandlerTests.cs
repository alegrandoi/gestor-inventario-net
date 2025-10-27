using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Inventory.Commands;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestorInventario.Application.Tests.Inventory;

public class AdjustInventoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenOutQuantityExceedsAvailable()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrow_WhenOutQuantityExceedsAvailable));
        var product = new Product
        {
            Code = "PRO-INV",
            Name = "Producto Inventario",
            Currency = "EUR",
            DefaultPrice = 10m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant { Product = product, ProductId = product.Id, Sku = "SKU-INV", Attributes = "size=S", TenantId = 1 };
        var warehouse = new Warehouse { Name = "Central", TenantId = 1 };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 5, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var handler = new AdjustInventoryCommandHandler(context, publisher);

        var command = new AdjustInventoryCommand(
            VariantId: variant.Id,
            WarehouseId: warehouse.Id,
            TransactionType: InventoryTransactionType.Out,
            Quantity: 10,
            MinStockLevel: null,
            DestinationWarehouseId: null,
            ReferenceType: null,
            ReferenceId: null,
            UserId: null,
            Notes: null);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldMoveStockAndPublishEvent_WhenValidMove()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldMoveStockAndPublishEvent_WhenValidMove));
        var product = new Product
        {
            Code = "PRO-MOVE",
            Name = "Producto Traslado",
            Currency = "EUR",
            DefaultPrice = 15m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant { Product = product, ProductId = product.Id, Sku = "SKU-MOVE", Attributes = "color=rojo", TenantId = 1 };
        var source = new Warehouse { Name = "Origen", TenantId = 1 };
        var destination = new Warehouse { Name = "Destino", TenantId = 1 };
        var sourceStock = new InventoryStock { Variant = variant, Warehouse = source, Quantity = 20, ReservedQuantity = 0, MinStockLevel = 0 };
        var destinationStock = new InventoryStock { Variant = variant, Warehouse = destination, Quantity = 5, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.AddRange(source, destination);
        context.InventoryStocks.AddRange(sourceStock, destinationStock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var events = new List<InventoryAdjustedDomainEvent>();
        publisher.RegisterHandler<InventoryAdjustedDomainEvent>((notification, _) =>
        {
            events.Add(notification);
            return Task.CompletedTask;
        });

        var handler = new AdjustInventoryCommandHandler(context, publisher);
        var command = new AdjustInventoryCommand(
            VariantId: variant.Id,
            WarehouseId: source.Id,
            TransactionType: InventoryTransactionType.Move,
            Quantity: 8,
            MinStockLevel: null,
            DestinationWarehouseId: destination.Id,
            ReferenceType: null,
            ReferenceId: null,
            UserId: null,
            Notes: "Traslado manual");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().HaveCount(2);
        var updatedSource = await context.InventoryStocks.SingleAsync(s => s.WarehouseId == source.Id);
        var updatedDestination = await context.InventoryStocks.SingleAsync(s => s.WarehouseId == destination.Id);
        updatedSource.Quantity.Should().Be(12);
        updatedDestination.Quantity.Should().Be(13);

        events.Should().ContainSingle();
        var movement = events.Single();
        movement.TransactionType.Should().Be(InventoryTransactionType.Move);
        movement.DestinationWarehouseId.Should().Be(destination.Id);
        movement.Adjustments.Should().HaveCount(2);
        movement.Adjustments.First(adj => adj.WarehouseId == source.Id).QuantityAfter.Should().Be(12);
        movement.Adjustments.First(adj => adj.WarehouseId == destination.Id).QuantityAfter.Should().Be(13);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenMoveUsesSameWarehouse()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrow_WhenMoveUsesSameWarehouse));
        var product = new Product
        {
            Code = "PRO-MOVE-ERR",
            Name = "Producto Error",
            Currency = "EUR",
            DefaultPrice = 12m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant { Product = product, ProductId = product.Id, Sku = "SKU-ERR", Attributes = "size=L", TenantId = 1 };
        var warehouse = new Warehouse { Name = "Único", TenantId = 1 };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 10, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var handler = new AdjustInventoryCommandHandler(context, publisher);

        var command = new AdjustInventoryCommand(
            VariantId: variant.Id,
            WarehouseId: warehouse.Id,
            TransactionType: InventoryTransactionType.Move,
            Quantity: 3,
            MinStockLevel: null,
            DestinationWarehouseId: warehouse.Id,
            ReferenceType: null,
            ReferenceId: null,
            UserId: null,
            Notes: null);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCreatePurchaseOrder_WhenManualInboundTransaction()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldCreatePurchaseOrder_WhenManualInboundTransaction));
        var product = new Product
        {
            Code = "PRO-PUR",
            Name = "Producto Compra",
            Currency = "EUR",
            DefaultPrice = 18m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-PUR",
            Attributes = "size=M",
            Price = null,
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Almacén Norte", TenantId = 1 };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var handler = new AdjustInventoryCommandHandler(context, publisher);

        var command = new AdjustInventoryCommand(
            VariantId: variant.Id,
            WarehouseId: warehouse.Id,
            TransactionType: InventoryTransactionType.In,
            Quantity: 7,
            MinStockLevel: null,
            DestinationWarehouseId: null,
            ReferenceType: null,
            ReferenceId: null,
            UserId: null,
            Notes: "Compra directa a proveedor local");

        await handler.Handle(command, CancellationToken.None);

        var purchaseOrder = await context.PurchaseOrders
            .Include(o => o.Lines)
            .Include(o => o.Supplier)
            .SingleAsync();

        purchaseOrder.Status.Should().Be(PurchaseOrderStatus.Received);
        purchaseOrder.Currency.Should().Be("EUR");
        purchaseOrder.TotalAmount.Should().Be(7 * product.DefaultPrice);
        purchaseOrder.Notes.Should().Contain("Compra directa a proveedor local");
        purchaseOrder.Supplier.Should().NotBeNull();
        purchaseOrder.Supplier!.Name.Should().Be("Proveedor movimientos manuales");
        purchaseOrder.Lines.Should().ContainSingle();
        var line = purchaseOrder.Lines.Single();
        line.Quantity.Should().Be(7);
        line.UnitPrice.Should().Be(product.DefaultPrice);
        line.TotalLine.Should().Be(7 * product.DefaultPrice);

        var suppliers = await context.Suppliers.ToListAsync();
        suppliers.Should().ContainSingle(s => s.Name == "Proveedor movimientos manuales");
    }

    [Fact]
    public async Task Handle_ShouldCreateSalesOrder_WhenManualOutboundTransaction()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldCreateSalesOrder_WhenManualOutboundTransaction));
        var product = new Product
        {
            Code = "PRO-SALE",
            Name = "Producto Venta",
            Currency = "EUR",
            DefaultPrice = 22m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-SALE",
            Attributes = "color=azul",
            Price = 25m,
            TenantId = 1
        };
        var warehouse = new Warehouse { Name = "Almacén Sur", TenantId = 1 };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 30, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var handler = new AdjustInventoryCommandHandler(context, publisher);

        var command = new AdjustInventoryCommand(
            VariantId: variant.Id,
            WarehouseId: warehouse.Id,
            TransactionType: InventoryTransactionType.Out,
            Quantity: 5,
            MinStockLevel: null,
            DestinationWarehouseId: null,
            ReferenceType: null,
            ReferenceId: null,
            UserId: null,
            Notes: "Venta mostrador urgente");

        await handler.Handle(command, CancellationToken.None);

        var salesOrder = await context.SalesOrders
            .Include(o => o.Lines)
            .Include(o => o.Customer)
            .SingleAsync();

        salesOrder.Status.Should().Be(SalesOrderStatus.Delivered);
        salesOrder.Currency.Should().Be("EUR");
        salesOrder.TotalAmount.Should().Be(5 * variant.Price!.Value);
        salesOrder.Notes.Should().Contain("Venta mostrador urgente");
        salesOrder.Customer.Should().NotBeNull();
        salesOrder.Customer!.Name.Should().Be("Cliente movimientos manuales");
        salesOrder.Lines.Should().ContainSingle();
        var salesLine = salesOrder.Lines.Single();
        salesLine.Quantity.Should().Be(5);
        salesLine.UnitPrice.Should().Be(variant.Price.Value);
        salesLine.TotalLine.Should().Be(5 * variant.Price.Value);

        var customers = await context.Customers.ToListAsync();
        customers.Should().ContainSingle(c => c.Name == "Cliente movimientos manuales");
    }
}
