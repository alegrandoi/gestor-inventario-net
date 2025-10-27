using System;
using FluentAssertions;
using GestorInventario.Application.Analytics.Queries;
using GestorInventario.Application.Analytics.Services;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Domain.Entities;
using Xunit;

namespace GestorInventario.Application.Tests.Analytics;

public class GetDemandForecastQueryTests
{
    [Fact]
    public async Task Handle_ShouldComputeMovingAverageForecast()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateContext(nameof(Handle_ShouldComputeMovingAverageForecast));

        var product = new Product { Code = "SKU-200", Name = "Chaqueta térmica", DefaultPrice = 120, Currency = "EUR", IsActive = true };
        var variant = new ProductVariant { Product = product, Sku = "SKU-200-NEGRO-M", Attributes = "color=negro;talla=M" };

        context.ProductVariants.Add(variant);
        context.Products.Add(product);

        var now = DateTime.UtcNow;
        var history = new List<DemandHistory>
        {
            new() { Variant = variant, Date = new DateTime(now.Year, now.Month, 1).AddMonths(-3), Quantity = 50 },
            new() { Variant = variant, Date = new DateTime(now.Year, now.Month, 1).AddMonths(-2), Quantity = 40 },
            new() { Variant = variant, Date = new DateTime(now.Year, now.Month, 1).AddMonths(-1), Quantity = 60 }
        };

        context.DemandHistory.AddRange(history);
        await context.SaveChangesAsync();

        var demandForecastService = new DemandForecastService();
        var handler = new GetDemandForecastQueryHandler(context, demandForecastService);

        // Act
        var result = await handler.Handle(new GetDemandForecastQuery(variant.Id, 2), CancellationToken.None);

        // Assert
        result.Forecast.Should().HaveCount(2);
        result.Forecast
            .Select(point => point.Quantity)
            .Should()
            .OnlyContain(quantity => Math.Abs(quantity - 50.9m) < 0.2m);
    }
}
