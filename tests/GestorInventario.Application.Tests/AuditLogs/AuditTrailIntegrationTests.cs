using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GestorInventario.Application.Auditing.EventHandlers;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Models;
using GestorInventario.Application.Inventory.Commands;
using GestorInventario.Application.Products.Commands;
using GestorInventario.Application.Tests.Helpers;
using GestorInventario.Application.Users.Commands;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using GestorInventario.Infrastructure.Auditing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GestorInventario.Application.Tests.AuditLogs;

public class AuditTrailIntegrationTests
{
    [Fact]
    public async Task CreateProductCommand_ShouldGenerateAuditLogEntry()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(CreateProductCommand_ShouldGenerateAuditLogEntry));
        var currentUser = new StubCurrentUserService(5, "auditor");
        var auditTrail = new AuditTrailInterceptor(context, currentUser, NullLogger<AuditTrailInterceptor>.Instance);

        var publisher = new TestPublisher();
        var productCreatedHandler = new ProductCreatedDomainEventHandler(auditTrail);
        publisher.RegisterHandler<ProductCreatedDomainEvent>(productCreatedHandler.Handle);

        var handler = new CreateProductCommandHandler(context, publisher);

        var command = new CreateProductCommand(
            Code: "SKU-AUDIT",
            Name: "Producto Auditado",
            Description: "Alta para auditoría",
            CategoryId: null,
            DefaultPrice: 42.5m,
            Currency: "EUR",
            TaxRateId: null,
            IsActive: true,
            WeightKg: 1.2m,
            HeightCm: null,
            WidthCm: null,
            LengthCm: null,
            LeadTimeDays: null,
            SafetyStock: null,
            ReorderPoint: null,
            ReorderQuantity: null,
            RequiresSerialTracking: false,
            Variants:
            [
                new CreateProductVariantRequest("SKU-AUDIT-01", "color=azul", null, null)
            ],
            Images: Array.Empty<CreateProductImageRequest>());

        var result = await handler.Handle(command, CancellationToken.None);

        var auditLog = context.AuditLogs.Should().ContainSingle().Subject;
        auditLog.EntityName.Should().Be("Product");
        auditLog.EntityId.Should().Be(result.Id);
        auditLog.Action.Should().Be("ProductCreated");
        auditLog.UserId.Should().Be(currentUser.UserId);
        auditLog.Changes.Should().NotBeNull();

        using var payload = JsonDocument.Parse(auditLog.Changes!);
        var root = payload.RootElement;
        root.GetProperty("metadata").GetProperty("performedBy").GetString().Should().Be("auditor");
        var changes = root.GetProperty("changes");
        changes.GetProperty("code").GetProperty("new").GetString().Should().Be("SKU-AUDIT");
        changes.GetProperty("code").TryGetProperty("old", out _).Should().BeFalse();
        changes.GetProperty("defaultPrice").GetProperty("new").GetDecimal().Should().Be(42.5m);
    }

    [Fact]
    public async Task AdjustInventoryCommand_ShouldSerializeQuantityDifferences()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(AdjustInventoryCommand_ShouldSerializeQuantityDifferences));
        var currentUser = new StubCurrentUserService(7, "inventory-manager");
        var auditTrail = new AuditTrailInterceptor(context, currentUser, NullLogger<AuditTrailInterceptor>.Instance);

        var publisher = new TestPublisher();
        var inventoryHandler = new InventoryAdjustedDomainEventHandler(auditTrail);
        publisher.RegisterHandler<InventoryAdjustedDomainEvent>(inventoryHandler.Handle);

        var product = new Product
        {
            Code = "PROD-100",
            Name = "Producto de inventario",
            Currency = "EUR",
            DefaultPrice = 15m
        };

        var variant = new ProductVariant
        {
            Product = product,
            Sku = "SKU-INV-001",
            Attributes = "color=azul"
        };

        product.Variants.Add(variant);

        var warehouse = new Warehouse
        {
            Name = "Central"
        };

        context.Products.Add(product);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var handler = new AdjustInventoryCommandHandler(context, publisher);

        var command = new AdjustInventoryCommand(
            VariantId: variant.Id,
            WarehouseId: warehouse.Id,
            TransactionType: InventoryTransactionType.In,
            Quantity: 10,
            MinStockLevel: null,
            DestinationWarehouseId: null,
            ReferenceType: "PurchaseOrder",
            ReferenceId: 25,
            UserId: currentUser.UserId,
            Notes: "Recepción de proveedor");

        await handler.Handle(command, CancellationToken.None);

        var auditLog = context.AuditLogs.Should().ContainSingle().Subject;
        auditLog.Action.Should().Be("InventoryAdjusted");
        auditLog.EntityName.Should().Be("InventoryStock");
        auditLog.EntityId.Should().Be(variant.Id);
        auditLog.Changes.Should().NotBeNull();

        using var payload = JsonDocument.Parse(auditLog.Changes!);
        var changes = payload.RootElement.GetProperty("changes");
        var warehouseKey = $"warehouse:{warehouse.Id}";
        var warehouseChange = changes.GetProperty(warehouseKey);
        warehouseChange.GetProperty("old").GetDecimal().Should().Be(0);
        warehouseChange.GetProperty("new").GetDecimal().Should().Be(10);

        var metadataUser = payload.RootElement.GetProperty("metadata").GetProperty("performedBy").GetString();
        metadataUser.Should().Be("inventory-manager");
    }

    [Fact]
    public async Task DeleteProductCommand_ShouldGenerateDeletionAuditLogEntry()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(DeleteProductCommand_ShouldGenerateDeletionAuditLogEntry));
        var currentUser = new StubCurrentUserService(11, "deleter");
        var auditTrail = new AuditTrailInterceptor(context, currentUser, NullLogger<AuditTrailInterceptor>.Instance);

        var publisher = new TestPublisher();
        var handler = new ProductDeletedDomainEventHandler(auditTrail);
        publisher.RegisterHandler<ProductDeletedDomainEvent>(handler.Handle);

        var product = new Product
        {
            Code = "SKU-DELETE",
            Name = "Producto Eliminado",
            Currency = "USD",
            DefaultPrice = 89.99m,
            IsActive = true
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var commandHandler = new DeleteProductCommandHandler(context, publisher);
        await commandHandler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        var auditLog = context.AuditLogs.Should().ContainSingle().Subject;
        auditLog.EntityName.Should().Be("Product");
        auditLog.EntityId.Should().Be(product.Id);
        auditLog.Action.Should().Be("ProductDeleted");
        auditLog.UserId.Should().Be(currentUser.UserId);
        auditLog.Changes.Should().NotBeNull();

        using var payload = JsonDocument.Parse(auditLog.Changes!);
        var root = payload.RootElement;
        root.GetProperty("description").GetString().Should().Be($"Baja de producto {product.Code}");

        var metadata = root.GetProperty("metadata");
        metadata.GetProperty("performedBy").GetString().Should().Be("deleter");
        metadata.GetProperty("performedById").GetInt32().Should().Be(currentUser.UserId!.Value);

        var changes = root.GetProperty("changes");
        var codeChange = changes.GetProperty("code");
        codeChange.GetProperty("old").GetString().Should().Be(product.Code);
        codeChange.TryGetProperty("new", out _).Should().BeFalse();

        var priceChange = changes.GetProperty("defaultPrice");
        priceChange.GetProperty("old").GetDecimal().Should().Be(product.DefaultPrice);
        priceChange.TryGetProperty("new", out _).Should().BeFalse();

        var activeChange = changes.GetProperty("isActive");
        activeChange.GetProperty("old").GetBoolean().Should().BeTrue();
        activeChange.TryGetProperty("new", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserRoleCommand_ShouldGenerateAuditLogEntry()
    {
        using var context = TestDbContextFactory.CreateContext(nameof(UpdateUserRoleCommand_ShouldGenerateAuditLogEntry));
        var currentUser = new StubCurrentUserService(13, "admin-user");
        var auditTrail = new AuditTrailInterceptor(context, currentUser, NullLogger<AuditTrailInterceptor>.Instance);

        var publisher = new TestPublisher();
        var handler = new UserRoleChangedDomainEventHandler(auditTrail);
        publisher.RegisterHandler<UserRoleChangedDomainEvent>(handler.Handle);

        var identityServiceMock = new Mock<IIdentityService>();
        var existingUser = new UserSummaryDto(21, "marcela", "marcela@example.com", "Operador", true);
        var updatedUser = existingUser with { Role = "Administrador" };

        identityServiceMock
            .Setup(service => service.GetByIdAsync(existingUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        identityServiceMock
            .Setup(service => service.UpdateUserRoleAsync(existingUser.Id, updatedUser.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserSummaryDto>.Success(updatedUser));

        var commandHandler = new UpdateUserRoleCommandHandler(identityServiceMock.Object, publisher);
        var command = new UpdateUserRoleCommand(existingUser.Id, updatedUser.Role);

        var result = await commandHandler.Handle(command, CancellationToken.None);
        result.Should().Be(updatedUser);

        var auditLog = context.AuditLogs.Should().ContainSingle().Subject;
        auditLog.Action.Should().Be("UserRoleChanged");
        auditLog.EntityName.Should().Be("User");
        auditLog.EntityId.Should().Be(existingUser.Id);
        auditLog.UserId.Should().Be(currentUser.UserId);
        auditLog.Changes.Should().NotBeNull();

        using var payload = JsonDocument.Parse(auditLog.Changes!);
        var root = payload.RootElement;
        root.GetProperty("description").GetString().Should().Be($"Cambio de rol para {existingUser.Username}");

        var metadata = root.GetProperty("metadata");
        metadata.GetProperty("performedBy").GetString().Should().Be("admin-user");
        metadata.GetProperty("performedById").GetInt32().Should().Be(currentUser.UserId!.Value);

        var changes = root.GetProperty("changes");
        var roleChange = changes.GetProperty("role");
        roleChange.GetProperty("old").GetString().Should().Be(existingUser.Role);
        roleChange.GetProperty("new").GetString().Should().Be(updatedUser.Role);
    }
}
