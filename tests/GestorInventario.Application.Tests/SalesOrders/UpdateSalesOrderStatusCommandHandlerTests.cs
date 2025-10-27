using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.SalesOrders.Commands;
using GestorInventario.Application.SalesOrders.Events;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Tests.SalesOrders;

public class UpdateSalesOrderStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReduceStock_WhenOrderIsShipped()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldReduceStock_WhenOrderIsShipped));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product
        {
            Code = "PRO-ORDER",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 10m,
            WeightKg = 1,
            RequiresSerialTracking = false
        };
        var variant = new ProductVariant { Sku = "SKU-ORDER", Attributes = "size=M", Product = product };
        var warehouse = new Warehouse { Name = "Central" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 15, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var inventoryEvents = new List<InventoryAdjustedDomainEvent>();
        publisher.RegisterHandler<InventoryAdjustedDomainEvent>((notification, _) =>
        {
            inventoryEvents.Add(notification);
            return Task.CompletedTask;
        });
        var createHandler = new CreateSalesOrderCommandHandler(context, publisher);
        var createCommand = new CreateSalesOrderCommand(
            CustomerId: customer.Id,
            OrderDate: DateTime.UtcNow,
            Status: SalesOrderStatus.Confirmed,
            ShippingAddress: null,
            Currency: "EUR",
            Notes: null,
            CarrierId: null,
            EstimatedDeliveryDate: null,
            Lines: new[]
            {
                new CreateSalesOrderLineRequest(variant.Id, 5, 50m, null, null)
            });

        var orderResult = await createHandler.Handle(createCommand, CancellationToken.None);
        var order = await context.SalesOrders
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
            .FirstAsync(o => o.Id == orderResult.Id);

        stock.ReservedQuantity.Should().Be(5);

        var handler = new UpdateSalesOrderStatusCommandHandler(context, publisher);
        var command = new UpdateSalesOrderStatusCommand(
            OrderId: order.Id,
            Status: SalesOrderStatus.Shipped,
            Allocations: new[]
            {
                new SalesOrderShipmentAllocation(variant.Id, warehouse.Id, 5)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(SalesOrderStatus.Shipped);
        context.InventoryStocks.Single().Quantity.Should().Be(10);
        context.InventoryStocks.Single().ReservedQuantity.Should().Be(0);
        context.InventoryTransactions.Should().ContainSingle(tran => tran.TransactionType == InventoryTransactionType.Out);
        inventoryEvents.Should().ContainSingle();
        var raisedEvent = inventoryEvents.Single();
        raisedEvent.TransactionType.Should().Be(InventoryTransactionType.Out);
        raisedEvent.Quantity.Should().Be(5);
        raisedEvent.Adjustments.Should().ContainSingle();
        var detail = raisedEvent.Adjustments.Single();
        detail.QuantityBefore.Should().Be(15);
        detail.QuantityAfter.Should().Be(10);
        raisedEvent.ReferenceType.Should().Be(nameof(SalesOrder));
        raisedEvent.ReferenceId.Should().Be(order.Id);

        result.Shipments.Should().ContainSingle();
        var shipmentDto = result.Shipments.Single();
        shipmentDto.Status.Should().Be(ShipmentStatus.InTransit);
        shipmentDto.WarehouseId.Should().Be(warehouse.Id);
        shipmentDto.EstimatedDeliveryDate.Should().NotBeNull();

        var persistedShipment = await context.Shipments.Include(s => s.Lines).SingleAsync();
        persistedShipment.Status.Should().Be(ShipmentStatus.InTransit);
        persistedShipment.ShippedAt.Should().NotBeNull();
        persistedShipment.TotalWeight.Should().Be(5m);
        persistedShipment.Lines.Should().ContainSingle(line => line.Quantity == 5 && line.SalesOrderLineId == order.Lines.Single().Id);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenShippingWithoutAllocations()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrow_WhenShippingWithoutAllocations));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product { Code = "PRO-002", Name = "Producto", Currency = "EUR", DefaultPrice = 12m, WeightKg = 1, RequiresSerialTracking = false };
        var variant = new ProductVariant { Sku = "SKU-002", Attributes = "size=L", Product = product };
        var warehouse = new Warehouse { Name = "Principal" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 20, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var createHandler = new CreateSalesOrderCommandHandler(context, publisher);
        var orderResult = await createHandler.Handle(
            new CreateSalesOrderCommand(
                customer.Id,
                DateTime.UtcNow,
                SalesOrderStatus.Confirmed,
                null,
                "EUR",
                null,
                null,
                null,
                new[] { new CreateSalesOrderLineRequest(variant.Id, 4, 48m, null, null) }),
            CancellationToken.None);

        var handler = new UpdateSalesOrderStatusCommandHandler(context, publisher);
        var command = new UpdateSalesOrderStatusCommand(orderResult.Id, SalesOrderStatus.Shipped, Array.Empty<SalesOrderShipmentAllocation>());

        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*Allocations are required when shipping an order.*");
    }

    [Fact]
    public async Task Handle_ShouldMarkOrderAsDelivered_WhenNoPendingAllocationsRemain()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldMarkOrderAsDelivered_WhenNoPendingAllocationsRemain));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product { Code = "PRO-003", Name = "Producto", Currency = "EUR", DefaultPrice = 15m, WeightKg = 1, RequiresSerialTracking = false };
        var variant = new ProductVariant { Sku = "SKU-003", Attributes = "color=azul", Product = product };
        var warehouse = new Warehouse { Name = "Secundario" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 10, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var statusEvents = new List<SalesOrderStatusChangedDomainEvent>();
        publisher.RegisterHandler<SalesOrderStatusChangedDomainEvent>((notification, _) =>
        {
            statusEvents.Add(notification);
            return Task.CompletedTask;
        });

        var createHandler = new CreateSalesOrderCommandHandler(context, publisher);
        var orderResult = await createHandler.Handle(
            new CreateSalesOrderCommand(
                customer.Id,
                DateTime.UtcNow,
                SalesOrderStatus.Confirmed,
                null,
                "EUR",
                null,
                null,
                null,
                new[] { new CreateSalesOrderLineRequest(variant.Id, 3, 45m, null, null) }),
            CancellationToken.None);

        var handler = new UpdateSalesOrderStatusCommandHandler(context, publisher);

        await handler.Handle(
            new UpdateSalesOrderStatusCommand(
                orderResult.Id,
                SalesOrderStatus.Shipped,
                new[] { new SalesOrderShipmentAllocation(variant.Id, warehouse.Id, 3) }),
            CancellationToken.None);

        var shipmentsAfterShip = await context.Shipments.ToListAsync();
        shipmentsAfterShip.Should().ContainSingle();
        shipmentsAfterShip.Single().Status.Should().Be(ShipmentStatus.InTransit);

        var deliverResult = await handler.Handle(
            new UpdateSalesOrderStatusCommand(
                orderResult.Id,
                SalesOrderStatus.Delivered,
                Array.Empty<SalesOrderShipmentAllocation>()),
            CancellationToken.None);

        deliverResult.Status.Should().Be(SalesOrderStatus.Delivered);
        deliverResult.Shipments.Should().AllSatisfy(shipment => shipment.Status.Should().Be(ShipmentStatus.Delivered));
        statusEvents.Should().HaveCount(2);
        statusEvents.Last().NewStatus.Should().Be(SalesOrderStatus.Delivered);

        var allocations = await context.SalesOrderAllocations.ToListAsync();
        allocations.Should().AllSatisfy(allocation =>
        {
            allocation.Status.Should().Be(SalesOrderAllocationStatus.Delivered);
            allocation.ReleasedAt.Should().NotBeNull();
        });

        var shipmentsAfterDelivery = await context.Shipments.ToListAsync();
        shipmentsAfterDelivery.Should().ContainSingle();
        shipmentsAfterDelivery.Single().Status.Should().Be(ShipmentStatus.Delivered);
        shipmentsAfterDelivery.Single().DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReleaseAllocations_WhenOrderIsCancelled()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldReleaseAllocations_WhenOrderIsCancelled));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product { Code = "PRO-004", Name = "Producto", Currency = "EUR", DefaultPrice = 18m, WeightKg = 1, RequiresSerialTracking = false };
        var variant = new ProductVariant { Sku = "SKU-004", Attributes = "color=rojo", Product = product };
        var warehouse = new Warehouse { Name = "Central" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 6, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var createHandler = new CreateSalesOrderCommandHandler(context, publisher);
        var orderResult = await createHandler.Handle(
            new CreateSalesOrderCommand(
                customer.Id,
                DateTime.UtcNow,
                SalesOrderStatus.Pending,
                null,
                "EUR",
                null,
                null,
                null,
                new[] { new CreateSalesOrderLineRequest(variant.Id, 4, 40m, null, null) }),
            CancellationToken.None);

        var handler = new UpdateSalesOrderStatusCommandHandler(context, publisher);
        var cancelResult = await handler.Handle(
            new UpdateSalesOrderStatusCommand(orderResult.Id, SalesOrderStatus.Cancelled, Array.Empty<SalesOrderShipmentAllocation>()),
            CancellationToken.None);

        cancelResult.Status.Should().Be(SalesOrderStatus.Cancelled);

        var stockAfter = await context.InventoryStocks.SingleAsync();
        stockAfter.ReservedQuantity.Should().Be(0);

        var allocations = await context.SalesOrderAllocations.ToListAsync();
        allocations.Should().AllSatisfy(allocation =>
        {
            allocation.Status.Should().Be(SalesOrderAllocationStatus.Released);
            allocation.ReleasedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransitionIsNotAllowed()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldThrow_WhenTransitionIsNotAllowed));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product { Code = "PRO-005", Name = "Producto", Currency = "EUR", DefaultPrice = 11m, WeightKg = 1, RequiresSerialTracking = false };
        var variant = new ProductVariant { Sku = "SKU-005", Attributes = "size=S", Product = product };
        var warehouse = new Warehouse { Name = "Central" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 5, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var createHandler = new CreateSalesOrderCommandHandler(context, publisher);
        var orderResult = await createHandler.Handle(
            new CreateSalesOrderCommand(
                customer.Id,
                DateTime.UtcNow,
                SalesOrderStatus.Pending,
                null,
                "EUR",
                null,
                null,
                null,
                new[] { new CreateSalesOrderLineRequest(variant.Id, 2, 22m, null, null) }),
            CancellationToken.None);

        var handler = new UpdateSalesOrderStatusCommandHandler(context, publisher);
        var command = new UpdateSalesOrderStatusCommand(orderResult.Id, SalesOrderStatus.Delivered, Array.Empty<SalesOrderShipmentAllocation>());

        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldNormalizeUnknownCurrentStatusBeforeValidating()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldNormalizeUnknownCurrentStatusBeforeValidating));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product { Code = "PRO-UNK-STATUS", Name = "Producto", Currency = "EUR", DefaultPrice = 20m, WeightKg = 1, RequiresSerialTracking = false };
        var variant = new ProductVariant { Sku = "SKU-UNK-STATUS", Attributes = "size=M", Product = product };
        var warehouse = new Warehouse { Name = "Principal" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 8, ReservedQuantity = 0, MinStockLevel = 0 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var publisher = new TestPublisher();
        var createHandler = new CreateSalesOrderCommandHandler(context, publisher);
        var createResult = await createHandler.Handle(
            new CreateSalesOrderCommand(
                customer.Id,
                DateTime.UtcNow,
                SalesOrderStatus.Pending,
                null,
                "EUR",
                null,
                null,
                null,
                new[] { new CreateSalesOrderLineRequest(variant.Id, 3, 60m, null, null) }),
            CancellationToken.None);

        var storedOrder = await context.SalesOrders.FirstAsync(o => o.Id == createResult.Id);
        storedOrder.Status = (SalesOrderStatus)0;
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new UpdateSalesOrderStatusCommandHandler(context, publisher);
        var result = await handler.Handle(
            new UpdateSalesOrderStatusCommand(
                createResult.Id,
                SalesOrderStatus.Confirmed,
                Array.Empty<SalesOrderShipmentAllocation>()),
            CancellationToken.None);

        result.Status.Should().Be(SalesOrderStatus.Confirmed);

        var refreshedOrder = await context.SalesOrders.FindAsync(createResult.Id);
        refreshedOrder!.Status.Should().Be(SalesOrderStatus.Confirmed);
    }
}
