using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Common.Interfaces;

public interface IGestorInventarioDbContext
{
    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<ProductImage> ProductImages { get; }

    DbSet<ProductVariant> ProductVariants { get; }

    DbSet<ProductAttributeGroup> ProductAttributeGroups { get; }

    DbSet<ProductAttributeValue> ProductAttributeValues { get; }

    DbSet<Warehouse> Warehouses { get; }

    DbSet<InventoryStock> InventoryStocks { get; }

    DbSet<InventoryTransaction> InventoryTransactions { get; }

    DbSet<WarehouseProductVariant> WarehouseProductVariants { get; }

    DbSet<Supplier> Suppliers { get; }

    DbSet<Carrier> Carriers { get; }

    DbSet<Customer> Customers { get; }

    DbSet<PurchaseOrder> PurchaseOrders { get; }

    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }

    DbSet<SalesOrder> SalesOrders { get; }

    DbSet<SalesOrderLine> SalesOrderLines { get; }

    DbSet<SalesOrderAllocation> SalesOrderAllocations { get; }

    DbSet<PriceList> PriceLists { get; }

    DbSet<ProductPrice> ProductPrices { get; }

    DbSet<TaxRate> TaxRates { get; }

    DbSet<ShippingRate> ShippingRates { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<DemandHistory> DemandHistory { get; }

    DbSet<DemandAggregate> DemandAggregates { get; }

    DbSet<SeasonalFactor> SeasonalFactors { get; }

    DbSet<AbcPolicy> AbcPolicies { get; }

    DbSet<VariantAbcClassification> VariantAbcClassifications { get; }

    DbSet<Shipment> Shipments { get; }

    DbSet<ShipmentLine> ShipmentLines { get; }

    DbSet<ShipmentEvent> ShipmentEvents { get; }

    DbSet<Tenant> Tenants { get; }

    DbSet<Branch> Branches { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
