using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestorInventario.Api.Tests;

public class TenantResolutionMiddlewareTests : IClassFixture<TestingWebApplicationFactory>
{
    private readonly TestingWebApplicationFactory factory;

    public TenantResolutionMiddlewareTests(TestingWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ShouldFallbackToDefaultTenantWhenHeaderIsInvalid()
    {
        int orderId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GestorInventarioDbContext>();
            await context.Database.EnsureCreatedAsync();

            var tenant = await context.Tenants.FindAsync(1);
            if (tenant is null)
            {
                tenant = new Tenant
                {
                    Name = "Default Tenant",
                    Code = "DEFAULT",
                    DefaultCulture = "es-ES",
                    DefaultCurrency = "EUR",
                    IsActive = true
                };

                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();
            }

            var customer = new Customer
            {
                Name = "Cliente de pruebas",
                Email = "cliente@example.com",
                TenantId = tenant.Id
            };

            var order = new SalesOrder
            {
                Customer = customer,
                OrderDate = DateTime.UtcNow,
                Status = SalesOrderStatus.Pending,
                Currency = "EUR",
                TotalAmount = 120m,
                TenantId = tenant.Id
            };

            context.Customers.Add(customer);
            context.SalesOrders.Add(order);
            await context.SaveChangesAsync();

            orderId = order.Id;
        }

        var client = factory.CreateClient();
        await AuthenticateAsync(client);

        client.DefaultRequestHeaders.Add("X-Tenant-Id", "9999");

        var response = await client.GetAsync("/api/salesorders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<SalesOrderDto>>();
        orders.Should().NotBeNull();
        orders!.Should().Contain(order => order.Id == orderId);
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
