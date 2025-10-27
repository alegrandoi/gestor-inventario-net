using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class Product : TenantEntity, IAggregateRoot
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public decimal DefaultPrice { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int? TaxRateId { get; set; }

    public TaxRate? TaxRate { get; set; }

    public bool IsActive { get; set; } = true;

    public decimal WeightKg { get; set; }

    public decimal? WidthCm { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? LengthCm { get; set; }

    public int? LeadTimeDays { get; set; }

    public decimal? SafetyStock { get; set; }

    public decimal? ReorderPoint { get; set; }

    public decimal? ReorderQuantity { get; set; }

    public bool RequiresSerialTracking { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
