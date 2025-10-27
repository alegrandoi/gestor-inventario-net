using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestorInventario.Api.Tests;

public class WarehouseProductVariantEndpointsTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory factory;

    public WarehouseProductVariantEndpointsTests(TestingWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ShouldManageWarehouseAssignmentsLifecycle()
    {
        int warehouseId;
        int variantId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GestorInventarioDbContext>();
            await context.Database.EnsureCreatedAsync();

            var tenant = await context.Tenants.FindAsync(1);
            if (tenant is null)
            {
                tenant = new Tenant { Name = "Tenant", Code = "TEN", IsActive = true };
                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();
            }

            var product = new Product
            {
                Code = "PRD-API",
                Name = "Producto API",
                Currency = "EUR",
                DefaultPrice = 15,
                RequiresSerialTracking = false,
                WeightKg = 1,
                TenantId = tenant.Id
            };
            var variant = new ProductVariant
            {
                Product = product,
                ProductId = product.Id,
                Sku = "SKU-API",
                Attributes = "color=negro",
                TenantId = tenant.Id
            };
            var warehouse = new Warehouse
            {
                Name = "Almacén API",
                Address = "Calle 1",
                Description = "Almacén de pruebas",
                TenantId = tenant.Id
            };

            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            warehouseId = warehouse.Id;
            variantId = variant.Id;
        }

        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");

        var createResponse = await client.PostAsJsonAsync($"/api/warehouses/{warehouseId}/product-variants", new
        {
            variantId,
            minimumQuantity = 5,
            targetQuantity = 9
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<WarehouseProductVariantAssignmentDto>();
        created.Should().NotBeNull();
        created!.VariantId.Should().Be(variantId);
        created.MinimumQuantity.Should().Be(5);

        var listResponse = await client.GetAsync($"/api/warehouses/{warehouseId}/product-variants");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<WarehouseProductVariantAssignmentDto[]>();
        list.Should().NotBeNull();
        list!.Should().ContainSingle();

        var updateResponse = await client.PutAsJsonAsync($"/api/warehouses/{warehouseId}/product-variants/{created.Id}", new
        {
            minimumQuantity = 6,
            targetQuantity = 12
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<WarehouseProductVariantAssignmentDto>();
        updated.Should().NotBeNull();
        updated!.TargetQuantity.Should().Be(12);

        var byVariantResponse = await client.GetAsync($"/api/warehouses/product-variants/by-variant/{variantId}");
        byVariantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var byVariant = await byVariantResponse.Content.ReadFromJsonAsync<WarehouseProductVariantAssignmentDto[]>();
        byVariant.Should().NotBeNull();
        byVariant!.Should().ContainSingle(dto => dto.WarehouseId == warehouseId);

        var deleteResponse = await client.DeleteAsync($"/api/warehouses/{warehouseId}/product-variants/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
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

    private sealed record LoginResponse(string? Token);
}
