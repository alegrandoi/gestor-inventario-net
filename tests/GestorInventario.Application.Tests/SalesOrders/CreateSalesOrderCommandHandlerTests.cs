using FluentAssertions;
using GestorInventario.Application.SalesOrders.Commands;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestorInventario.Application.Tests.SalesOrders;

public class CreateSalesOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateOrderAndReserveStock()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldCreateOrderAndReserveStock));

        var customer = new Customer
        {
            Name = "Cliente Demo"
        };

        var product = new Product
        {
            Code = "PROD-SO",
            Name = "Producto Venta",
            Currency = "EUR",
            DefaultPrice = 40m
        };

        var variant = new ProductVariant
        {
            Product = product,
            Sku = "SKU-SO",
            Attributes = "color=negro"
        };

        var warehouse = new Warehouse { Name = "Principal" };

        context.Customers.Add(customer);
        context.InventoryStocks.Add(new InventoryStock
        {
            Variant = variant,
            Warehouse = warehouse,
            Quantity = 30,
            ReservedQuantity = 0,
            MinStockLevel = 5
        });

        await context.SaveChangesAsync();

        var handler = new CreateSalesOrderCommandHandler(context, new TestPublisher());
        var command = new CreateSalesOrderCommand(
            CustomerId: customer.Id,
            OrderDate: DateTime.UtcNow,
            Status: SalesOrderStatus.Pending,
            ShippingAddress: null,
            Currency: "EUR",
            Notes: null,
            CarrierId: null,
            EstimatedDeliveryDate: null,
            Lines: new[]
            {
                new CreateSalesOrderLineRequest(
                    VariantId: variant.Id,
                    Quantity: 5,
                    UnitPrice: 40m,
                    Discount: 0m,
                    TaxRateId: null)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().BeGreaterThan(0);
        result.TotalAmount.Should().Be(200m);
        result.Lines.Should().ContainSingle();

        var stock = await context.InventoryStocks
            .SingleAsync(s => s.VariantId == variant.Id && s.WarehouseId == warehouse.Id);
        stock.ReservedQuantity.Should().Be(5);

        var savedOrder = await context.SalesOrders.FindAsync(result.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ShouldDefaultStatusToPending_WhenStatusIsUnknown()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldDefaultStatusToPending_WhenStatusIsUnknown));

        var customer = new Customer { Name = "Cliente" };
        var product = new Product { Code = "PRO-UNK", Name = "Producto", Currency = "EUR", DefaultPrice = 25m };
        var variant = new ProductVariant { Product = product, Sku = "SKU-UNK", Attributes = "color=verde" };
        var warehouse = new Warehouse { Name = "Central" };

        context.Customers.Add(customer);
        context.InventoryStocks.Add(new InventoryStock
        {
            Variant = variant,
            Warehouse = warehouse,
            Quantity = 10,
            ReservedQuantity = 0,
            MinStockLevel = 0
        });

        await context.SaveChangesAsync();

        var handler = new CreateSalesOrderCommandHandler(context, new TestPublisher());
        var command = new CreateSalesOrderCommand(
            CustomerId: customer.Id,
            OrderDate: DateTime.UtcNow,
            Status: (SalesOrderStatus)0,
            ShippingAddress: null,
            Currency: "EUR",
            Notes: null,
            CarrierId: null,
            EstimatedDeliveryDate: null,
            Lines: new[]
            {
                new CreateSalesOrderLineRequest(
                    VariantId: variant.Id,
                    Quantity: 2,
                    UnitPrice: 25m,
                    Discount: 0m,
                    TaxRateId: null)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(SalesOrderStatus.Pending);

        var storedOrder = await context.SalesOrders.AsNoTracking().SingleAsync();
        storedOrder.Status.Should().Be(SalesOrderStatus.Pending);
    }
}
