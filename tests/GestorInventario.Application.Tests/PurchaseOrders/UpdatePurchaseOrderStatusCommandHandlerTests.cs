using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.PurchaseOrders.Commands;
using GestorInventario.Application.PurchaseOrders.Events;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Tests.PurchaseOrders;

public class UpdatePurchaseOrderStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldIncreaseStockAndPublishEvent_WhenOrderIsReceived()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldIncreaseStockAndPublishEvent_WhenOrderIsReceived));

        var supplier = new Supplier { Name = "Proveedor", TenantId = 1 };
        var warehouse = new Warehouse { Name = "Principal", TenantId = 1 };
        var product = new Product
        {
            Code = "PRO-PURCHASE",
            Name = "Producto Compra",
            Currency = "EUR",
            DefaultPrice = 20m,
            WeightKg = 2,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-PURCHASE",
            Attributes = "color=azul",
            TenantId = 1
        };

        var order = new PurchaseOrder
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Ordered,
            TotalAmount = 80m,
            Currency = "EUR",
            TenantId = 1,
            Lines =
            {
                new PurchaseOrderLine
                {
                    Variant = variant,
                    VariantId = variant.Id,
                    Quantity = 8,
                    UnitPrice = 10m,
                    TotalLine = 80m,
                    TenantId = 1
                }
            }
        };

        context.Suppliers.Add(supplier);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var events = new List<InventoryAdjustedDomainEvent>();
        publisher.RegisterHandler<InventoryAdjustedDomainEvent>((notification, _) =>
        {
            events.Add(notification);
            return Task.CompletedTask;
        });

        var handler = new UpdatePurchaseOrderStatusCommandHandler(context, publisher);
        var command = new UpdatePurchaseOrderStatusCommand(order.Id, PurchaseOrderStatus.Received, warehouse.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(PurchaseOrderStatus.Received);
        var stock = await context.InventoryStocks.SingleAsync();
        stock.Quantity.Should().Be(8);
        context.InventoryTransactions.Should().ContainSingle(tran => tran.TransactionType == InventoryTransactionType.In);

        events.Should().ContainSingle();
        var raisedEvent = events.Single();
        raisedEvent.TransactionType.Should().Be(InventoryTransactionType.In);
        raisedEvent.Quantity.Should().Be(8);
        raisedEvent.Adjustments.Should().ContainSingle();
        var detail = raisedEvent.Adjustments.Single();
        detail.QuantityBefore.Should().Be(0);
        detail.QuantityAfter.Should().Be(8);
        raisedEvent.ReferenceType.Should().Be(nameof(PurchaseOrder));
        raisedEvent.ReferenceId.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenWarehouseIsMissingForReception()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrow_WhenWarehouseIsMissingForReception));

        var supplier = new Supplier { Name = "Proveedor", TenantId = 1 };
        var product = new Product
        {
            Code = "PRO-NOWH",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 10m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = "SKU-NOWH",
            Attributes = "size=M",
            TenantId = 1
        };
        var order = new PurchaseOrder
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Ordered,
            TotalAmount = 40m,
            Currency = "EUR",
            TenantId = 1,
            Lines = { new PurchaseOrderLine { Variant = variant, VariantId = variant.Id, Quantity = 4, UnitPrice = 10m, TotalLine = 40m, TenantId = 1 } }
        };

        context.Suppliers.Add(supplier);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync();

        var handler = new UpdatePurchaseOrderStatusCommandHandler(context, new TestPublisher());
        var command = new UpdatePurchaseOrderStatusCommand(order.Id, PurchaseOrderStatus.Received, null);

        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldPublishStatusChangedEvent_WhenStatusChanges()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldPublishStatusChangedEvent_WhenStatusChanges));

        var supplier = new Supplier { Name = "Proveedor", TenantId = 1 };
        var warehouse = new Warehouse { Name = "Central", TenantId = 1 };
        var product = new Product
        {
            Code = "PRO-STATUS",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 5m,
            WeightKg = 1,
            RequiresSerialTracking = false,
            TenantId = 1
        };
        var variant = new ProductVariant { Product = product, ProductId = product.Id, Sku = "SKU-STATUS", Attributes = "color=verde", TenantId = 1 };

        var order = new PurchaseOrder
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Pending,
            TotalAmount = 10m,
            Currency = "EUR",
            TenantId = 1,
            Lines = { new PurchaseOrderLine { Variant = variant, VariantId = variant.Id, Quantity = 2, UnitPrice = 5m, TotalLine = 10m, TenantId = 1 } }
        };

        context.Suppliers.Add(supplier);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var statusEvents = new List<PurchaseOrderStatusChangedDomainEvent>();
        publisher.RegisterHandler<PurchaseOrderStatusChangedDomainEvent>((notification, _) =>
        {
            statusEvents.Add(notification);
            return Task.CompletedTask;
        });

        var handler = new UpdatePurchaseOrderStatusCommandHandler(context, publisher);

        var orderedResult = await handler.Handle(
            new UpdatePurchaseOrderStatusCommand(order.Id, PurchaseOrderStatus.Ordered, null),
            CancellationToken.None);

        orderedResult.Status.Should().Be(PurchaseOrderStatus.Ordered);

        statusEvents.Should().ContainSingle();
        var statusEvent = statusEvents.Single();
        statusEvent.PreviousStatus.Should().Be(PurchaseOrderStatus.Pending);
        statusEvent.NewStatus.Should().Be(PurchaseOrderStatus.Ordered);

        var receivedResult = await handler.Handle(
            new UpdatePurchaseOrderStatusCommand(order.Id, PurchaseOrderStatus.Received, warehouse.Id),
            CancellationToken.None);

        receivedResult.Status.Should().Be(PurchaseOrderStatus.Received);
        statusEvents.Should().HaveCount(2);
        statusEvents.Last().NewStatus.Should().Be(PurchaseOrderStatus.Received);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransitionIsNotAllowed()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrow_WhenTransitionIsNotAllowed));

        var supplier = new Supplier { Name = "Proveedor", TenantId = 1 };
        var product = new Product { Code = "PRO-INVALID", Name = "Producto", Currency = "EUR", DefaultPrice = 9m, WeightKg = 1, RequiresSerialTracking = false, TenantId = 1 };
        var variant = new ProductVariant { Product = product, ProductId = product.Id, Sku = "SKU-INVALID", Attributes = "size=XL", TenantId = 1 };

        var order = new PurchaseOrder
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Ordered,
            TotalAmount = 18m,
            Currency = "EUR",
            TenantId = 1,
            Lines = { new PurchaseOrderLine { Variant = variant, VariantId = variant.Id, Quantity = 2, UnitPrice = 9m, TotalLine = 18m, TenantId = 1 } }
        };

        context.Suppliers.Add(supplier);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync();

        var handler = new UpdatePurchaseOrderStatusCommandHandler(context, new TestPublisher());
        var command = new UpdatePurchaseOrderStatusCommand(order.Id, PurchaseOrderStatus.Pending, null);

        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
    }
}
