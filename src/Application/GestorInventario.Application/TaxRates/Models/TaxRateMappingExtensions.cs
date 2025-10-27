using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.TaxRates.Models;

public static class TaxRateMappingExtensions
{
    public static TaxRateDto ToDto(this TaxRate taxRate) =>
        new(taxRate.Id, taxRate.Name, taxRate.Rate, taxRate.Region, taxRate.Description);
}
