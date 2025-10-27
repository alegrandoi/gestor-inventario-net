using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestorInventario.Api.Tests;

public class PurchaseOrdersEndpointsTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory factory;

    public PurchaseOrdersEndpointsTests(TestingWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ShouldReceivePurchaseOrder_WhenWarehouseIsProvided()
    {
        var (orderId, warehouseId) = await SeedPurchaseOrderAsync(PurchaseOrderStatus.Pending);

        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");

        var response = await client.PutAsJsonAsync($"/api/purchaseorders/{orderId}/status", new
        {
            orderId,
            status = nameof(PurchaseOrderStatus.Received),
            warehouseId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PurchaseOrderDto>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PurchaseOrderStatus.Received);
        payload.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldReceivePurchaseOrder_WhenOrderWasPersistedWithDefaultStatusValue()
    {
        var (orderId, warehouseId) = await SeedPurchaseOrderAsync(default);

        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");

        var response = await client.PutAsJsonAsync($"/api/purchaseorders/{orderId}/status", new
        {
            orderId,
            status = nameof(PurchaseOrderStatus.Received),
            warehouseId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PurchaseOrderDto>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PurchaseOrderStatus.Received);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = "admin",
            password = "Admin123$"
        });

        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.Token);
    }

    private async Task<(int OrderId, int WarehouseId)> SeedPurchaseOrderAsync(PurchaseOrderStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GestorInventarioDbContext>();
        await context.Database.EnsureCreatedAsync();

        var tenant = await context.Tenants.FindAsync(1);
        if (tenant is null)
        {
            tenant = new Tenant { Name = "Tenant", Code = "TEN", IsActive = true };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        var supplier = new Supplier
        {
            Name = $"Proveedor-{uniqueSuffix}",
            TenantId = tenant.Id
        };

        var product = new Product
        {
            Code = $"PRD-PO-{uniqueSuffix}",
            Name = "Producto",
            Currency = "EUR",
            DefaultPrice = 5,
            RequiresSerialTracking = false,
            WeightKg = 1,
            TenantId = tenant.Id
        };

        var variant = new ProductVariant
        {
            Product = product,
            ProductId = product.Id,
            Sku = $"SKU-PO-{uniqueSuffix}",
            Attributes = "color=rojo",
            TenantId = tenant.Id
        };

        var warehouse = new Warehouse
        {
            Name = $"Central-{uniqueSuffix}",
            Address = "Calle 123",
            TenantId = tenant.Id
        };

        var order = new PurchaseOrder
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = status,
            Currency = "EUR",
            TotalAmount = 10,
            TenantId = tenant.Id,
            Lines =
            {
                new PurchaseOrderLine
                {
                    Variant = variant,
                    VariantId = variant.Id,
                    Quantity = 2,
                    UnitPrice = 5,
                    TotalLine = 10,
                    TenantId = tenant.Id
                }
            }
        };

        context.Suppliers.Add(supplier);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.Warehouses.Add(warehouse);
        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync();

        return (order.Id, warehouse.Id);
    }

    private sealed record LoginResponse(string? Token);
}
