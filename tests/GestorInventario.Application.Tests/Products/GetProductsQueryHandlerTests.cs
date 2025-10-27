using FluentAssertions;
using GestorInventario.Application.Products.Queries;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using GestorInventario.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorInventario.Application.Tests.Products;

public class GetProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnFinalPriceWithAppliedTaxRate()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldReturnFinalPriceWithAppliedTaxRate));

        var taxRate = new TaxRate { Name = "IVA 21%", Rate = 21m };
        var product = new Product
        {
            Code = "SKU-001",
            Name = "Producto 1",
            DefaultPrice = 100m,
            Currency = "EUR",
            TaxRate = taxRate,
            IsActive = true,
            RequiresSerialTracking = false,
            WeightKg = 1m
        };

        context.AddRange(taxRate, product);
        await context.SaveChangesAsync();

        var cache = new InMemoryDistributedCache();
        var keyRegistry = new DistributedCacheKeyRegistry(cache);
        var handler = new GetProductsQueryHandler(
            context,
            cache,
            keyRegistry,
            NullLogger<GetProductsQueryHandler>.Instance);

        var result = await handler.Handle(new GetProductsQuery(null, null, null), CancellationToken.None);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(50);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);

        var dto = result.Items.Should().ContainSingle().Subject;
        dto.AppliedTaxRate.Should().Be(21m);
        dto.FinalPrice.Should().Be(121m);
    }
}
