using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class VariantAbcClassification : TenantEntity
{
    public int VariantId { get; set; }

    public ProductVariant? Variant { get; set; }

    public int AbcPolicyId { get; set; }

    public AbcPolicy? Policy { get; set; }

    public string Classification { get; set; } = "C";

    public decimal AnnualConsumptionValue { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
