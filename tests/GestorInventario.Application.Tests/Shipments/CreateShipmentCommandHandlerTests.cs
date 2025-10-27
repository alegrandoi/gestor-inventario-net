using FluentAssertions;
using GestorInventario.Application.SalesOrders.Commands;
using GestorInventario.Application.Shipments.Commands;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestorInventario.Application.Tests.Shipments;

public class CreateShipmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateShipmentAndConsumeReservation()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldCreateShipmentAndConsumeReservation));

        var customer = new Customer { Name = "Distribuidor" };
        var product = new Product
        {
            Code = "SHIP-001",
            Name = "Producto logístico",
            Currency = "EUR",
            DefaultPrice = 20m,
            WeightKg = 0.5m,
            RequiresSerialTracking = false
        };
        var variant = new ProductVariant { Product = product, Sku = "SHIP-001-A", Attributes = "color=azul" };
        var warehouse = new Warehouse { Name = "Almacén principal" };
        var stock = new InventoryStock { Variant = variant, Warehouse = warehouse, Quantity = 50, ReservedQuantity = 0, MinStockLevel = 5 };

        context.Customers.Add(customer);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.InventoryStocks.Add(stock);
        await context.SaveChangesAsync();

        var createOrderHandler = new CreateSalesOrderCommandHandler(context, new TestPublisher());
        var orderDto = await createOrderHandler.Handle(
            new CreateSalesOrderCommand(
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
                    new CreateSalesOrderLineRequest(variant.Id, 10, 30m, null, null)
                }),
            CancellationToken.None);

        var order = await context.SalesOrders
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
            .FirstAsync(o => o.Id == orderDto.Id);

        stock.ReservedQuantity.Should().Be(10);

        var handler = new CreateShipmentCommandHandler(context);
        var shipment = await handler.Handle(
            new CreateShipmentCommand(
                SalesOrderId: order.Id,
                WarehouseId: warehouse.Id,
                CarrierId: null,
                TrackingNumber: "TRK123",
                ShippedAt: DateTime.UtcNow,
                EstimatedDeliveryDate: DateTime.UtcNow.AddDays(2),
                TotalWeight: 5m,
                Notes: "Salida parcial",
                Lines: new[]
                {
                    new CreateShipmentLineRequest(order.Lines.Single().Id, 10, 5m)
                }),
            CancellationToken.None);

        shipment.Status.Should().Be(ShipmentStatus.InTransit);
        shipment.Lines.Should().HaveCount(1);

        var persistedStock = await context.InventoryStocks.Include(s => s.Variant).FirstAsync();
        persistedStock.Quantity.Should().Be(40);
        persistedStock.ReservedQuantity.Should().Be(0);

        var updatedOrder = await context.SalesOrders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(SalesOrderStatus.Shipped);
    }
}
