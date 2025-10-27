using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class ProductVariant : TenantEntity, IAggregateRoot
{
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Attributes { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public string? Barcode { get; set; }

    public ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();

    public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public ICollection<DemandHistory> DemandHistory { get; set; } = new List<DemandHistory>();

    public ICollection<DemandAggregate> DemandAggregates { get; set; } = new List<DemandAggregate>();

    public ICollection<SeasonalFactor> SeasonalFactors { get; set; } = new List<SeasonalFactor>();

    public ICollection<VariantAbcClassification> AbcClassifications { get; set; } = new List<VariantAbcClassification>();

    public ICollection<WarehouseProductVariant> WarehouseProductVariants { get; set; } = new List<WarehouseProductVariant>();
}
