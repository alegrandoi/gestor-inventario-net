using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Analytics.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using GestorInventario.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorInventario.Application.Tests.Analytics;

public class GetLogisticsDashboardQueryTests
{
    [Fact]
    public async Task Handle_ShouldAggregateOperationalIndicators()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldAggregateOperationalIndicators));
        var now = DateTime.UtcNow;

        var warehouseCentral = new Warehouse
        {
            Name = "Central"
        };

        var warehouseNorth = new Warehouse
        {
            Name = "Norte"
        };

        var carrierExpress = new Carrier
        {
            Name = "Logística Express"
        };

        var carrierRegional = new Carrier
        {
            Name = "Transporte Regional"
        };

        var product = new Product
        {
            Code = "SKU-200",
            Name = "Chaqueta técnica",
            SafetyStock = 3,
            LeadTimeDays = 2,
            ReorderPoint = 8,
            ReorderQuantity = 12,
            Currency = "EUR",
            DefaultPrice = 90,
            IsActive = true
        };

        var variant = new ProductVariant
        {
            Product = product,
            Sku = "SKU-200-GRN-M",
            Price = 95
        };

        var stock = new InventoryStock
        {
            Variant = variant,
            Warehouse = warehouseCentral,
            Quantity = 2,
            ReservedQuantity = 1
        };

        var customer = new Customer
        {
            Name = "Cliente corporativo",
            Email = "cliente@example.com"
        };

        var orderDelivered = new SalesOrder
        {
            Customer = customer,
            OrderDate = now.AddDays(-6),
            Status = SalesOrderStatus.Delivered,
            TotalAmount = 285,
            Currency = "EUR",
            Carrier = carrierExpress,
            EstimatedDeliveryDate = now.AddDays(-1)
        };

        var orderDeliveredLine = new SalesOrderLine
        {
            SalesOrder = orderDelivered,
            Variant = variant,
            Quantity = 3,
            UnitPrice = 95,
            TotalLine = 285
        };

        var allocationDelivered = new SalesOrderAllocation
        {
            SalesOrderLine = orderDeliveredLine,
            Warehouse = warehouseCentral,
            Quantity = 3,
            FulfilledQuantity = 3,
            Status = SalesOrderAllocationStatus.Delivered,
            ShippedAt = now.AddDays(-4)
        };

        orderDeliveredLine.Allocations.Add(allocationDelivered);

        var orderInTransit = new SalesOrder
        {
            Customer = customer,
            OrderDate = now.AddDays(-2),
            Status = SalesOrderStatus.Shipped,
            TotalAmount = 190,
            Currency = "EUR",
            Carrier = carrierExpress,
            EstimatedDeliveryDate = now.AddDays(3)
        };

        var orderInTransitLine = new SalesOrderLine
        {
            SalesOrder = orderInTransit,
            Variant = variant,
            Quantity = 4,
            UnitPrice = 95,
            TotalLine = 380
        };

        var allocationInTransit = new SalesOrderAllocation
        {
            SalesOrderLine = orderInTransitLine,
            Warehouse = warehouseNorth,
            Quantity = 4,
            FulfilledQuantity = 1,
            Status = SalesOrderAllocationStatus.Reserved,
            ShippedAt = now.AddDays(-1)
        };

        orderInTransitLine.Allocations.Add(allocationInTransit);

        var delayedShipment = new Shipment
        {
            SalesOrder = orderDelivered,
            Warehouse = warehouseCentral,
            Carrier = carrierExpress,
            TrackingNumber = "LX-001",
            Status = ShipmentStatus.Delivered,
            ShippedAt = now.AddDays(-3),
            DeliveredAt = now.AddDays(-1),
            EstimatedDeliveryDate = now.AddDays(-2),
            TotalWeight = 18.5m
        };
        delayedShipment.MarkCreated(now.AddDays(-4));

        var onTimeShipment = new Shipment
        {
            SalesOrder = orderDelivered,
            Warehouse = warehouseCentral,
            Carrier = carrierRegional,
            TrackingNumber = "TR-002",
            Status = ShipmentStatus.Delivered,
            ShippedAt = now.AddDays(-2),
            DeliveredAt = now.AddDays(-1),
            EstimatedDeliveryDate = now.AddDays(-1),
            TotalWeight = 12m
        };
        onTimeShipment.MarkCreated(now.AddDays(-2));

        var inTransitShipment = new Shipment
        {
            SalesOrder = orderInTransit,
            Warehouse = warehouseNorth,
            Carrier = carrierExpress,
            TrackingNumber = "LX-003",
            Status = ShipmentStatus.InTransit,
            ShippedAt = now.AddDays(-1),
            EstimatedDeliveryDate = now.AddDays(2),
            TotalWeight = 10m
        };
        inTransitShipment.MarkCreated(now.AddDays(-1));

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.InventoryStocks.Add(stock);
        context.Warehouses.AddRange(warehouseCentral, warehouseNorth);
        context.Carriers.AddRange(carrierExpress, carrierRegional);
        context.Customers.Add(customer);
        context.SalesOrders.AddRange(orderDelivered, orderInTransit);
        context.SalesOrderLines.AddRange(orderDeliveredLine, orderInTransitLine);
        context.SalesOrderAllocations.AddRange(allocationDelivered, allocationInTransit);
        context.Shipments.AddRange(delayedShipment, onTimeShipment, inTransitShipment);

        var demandEntries = Enumerable.Range(0, 5)
            .Select(offset => new DemandHistory
            {
                Variant = variant,
                Date = now.AddDays(-offset),
                Quantity = 5
            });

        context.DemandHistory.AddRange(demandEntries);

        await context.SaveChangesAsync();

        var cache = new InMemoryDistributedCache();
        var keyRegistry = new DistributedCacheKeyRegistry(cache);
        var handler = new GetLogisticsDashboardQueryHandler(
            context,
            cache,
            keyRegistry,
            NullLogger<GetLogisticsDashboardQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetLogisticsDashboardQuery(14), CancellationToken.None);

        // Assert
        result.TotalShipments.Should().Be(3);
        result.InTransitShipments.Should().Be(1);
        result.DeliveredShipments.Should().Be(2);
        result.OnTimeDeliveryRate.Should().BeApproximately(0.5, 0.0001);
        result.TopDelayedShipments.Should().ContainSingle(shipment => shipment.Id == delayedShipment.Id);
        result.UpcomingShipments.Should().ContainSingle(shipment => shipment.Status == ShipmentStatus.InTransit);
        result.WarehousePerformance.Should().Contain(performance =>
            performance.WarehouseId == warehouseCentral.Id &&
            performance.TotalShipments == 2 &&
            performance.OnTimeShipments == 1 &&
            performance.DelayedShipments == 1);
        result.CarrierPerformance.Should().Contain(performance =>
            performance.CarrierName == "Logística Express" &&
            performance.TotalShipments == 2);
        result.ShipmentVolumeTrend.Should().NotBeEmpty();
        result.TotalReplenishmentRecommendation.Should().BeGreaterThan(0);
    }
}
