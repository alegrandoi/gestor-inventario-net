using FluentAssertions;
using GestorInventario.Application.Products.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Products;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnFinalPriceAndAppliedTaxRate()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldReturnFinalPriceAndAppliedTaxRate));

        var taxRate = new TaxRate { Name = "IVA 19%", Rate = 19m };
        var product = new Product
        {
            Code = "SKU-002",
            Name = "Producto 2",
            DefaultPrice = 59.99m,
            Currency = "EUR",
            TaxRate = taxRate,
            IsActive = true,
            RequiresSerialTracking = false,
            WeightKg = 0.5m
        };

        context.AddRange(taxRate, product);
        await context.SaveChangesAsync();

        var handler = new GetProductByIdQueryHandler(context);
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.AppliedTaxRate.Should().Be(19m);
        result.FinalPrice.Should().Be(71.39m);
    }

    [Fact]
    public async Task Handle_ShouldUseBasePriceWhenNoTaxRateIsAssigned()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldUseBasePriceWhenNoTaxRateIsAssigned));

        var product = new Product
        {
            Code = "SKU-003",
            Name = "Producto 3",
            DefaultPrice = 45m,
            Currency = "EUR",
            TaxRateId = null,
            IsActive = true,
            RequiresSerialTracking = false,
            WeightKg = 0.75m
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var handler = new GetProductByIdQueryHandler(context);
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.AppliedTaxRate.Should().BeNull();
        result.FinalPrice.Should().Be(45m);
    }
}
