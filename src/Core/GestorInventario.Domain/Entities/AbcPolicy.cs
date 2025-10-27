using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class AbcPolicy : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal ThresholdA { get; set; }

    public decimal ThresholdB { get; set; }

    public decimal ServiceLevelA { get; set; }

    public decimal ServiceLevelB { get; set; }

    public decimal ServiceLevelC { get; set; }

    public ICollection<VariantAbcClassification> Classifications { get; set; } = new List<VariantAbcClassification>();
}
