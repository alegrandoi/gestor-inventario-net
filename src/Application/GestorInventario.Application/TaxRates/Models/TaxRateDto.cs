namespace GestorInventario.Application.TaxRates.Models;

public record TaxRateDto(int Id, string Name, decimal Rate, string? Region, string? Description);
