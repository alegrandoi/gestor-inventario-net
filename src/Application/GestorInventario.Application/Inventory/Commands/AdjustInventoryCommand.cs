using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Exceptions;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Commands;

public record AdjustInventoryCommand(
    int VariantId,
    int WarehouseId,
    InventoryTransactionType TransactionType,
    decimal Quantity,
    decimal? MinStockLevel,
    int? DestinationWarehouseId,
    string? ReferenceType,
    int? ReferenceId,
    int? UserId,
    string? Notes) : IRequest<IReadOnlyCollection<InventoryStockDto>>;

public class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(command => command.VariantId)
            .GreaterThan(0);

        RuleFor(command => command.WarehouseId)
            .GreaterThan(0);

        RuleFor(command => command.Quantity)
            .GreaterThan(0)
            .When(command => command.TransactionType is InventoryTransactionType.In or InventoryTransactionType.Out or InventoryTransactionType.Move);

        RuleFor(command => command.MinStockLevel)
            .GreaterThanOrEqualTo(0)
            .When(command => command.MinStockLevel.HasValue);

        RuleFor(command => command.DestinationWarehouseId)
            .GreaterThan(0)
            .When(command => command.TransactionType == InventoryTransactionType.Move);
    }
}

public class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, IReadOnlyCollection<InventoryStockDto>>
{
    private const string ManualSupplierName = "Proveedor movimientos manuales";
    private const string ManualCustomerName = "Cliente movimientos manuales";
    private const string ManualPartyNotes = "Generado automáticamente a partir de movimientos manuales de inventario.";

    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public AdjustInventoryCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<IReadOnlyCollection<InventoryStockDto>> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var variant = await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken)
            .ConfigureAwait(false);

        if (variant is null)
        {
            throw new NotFoundException(nameof(ProductVariant), request.VariantId);
        }

        var sourceWarehouse = await context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (sourceWarehouse is null)
        {
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);
        }

        var updatedStocks = new List<(InventoryStock stock, Warehouse warehouse, decimal quantityBefore)>();

        switch (request.TransactionType)
        {
            case InventoryTransactionType.In:
                {
                    var stock = await GetOrCreateStock(variant.Id, request.WarehouseId, cancellationToken).ConfigureAwait(false);
                    var quantityBefore = stock.Quantity;
                    stock.Quantity += request.Quantity;
                    UpdateMinStockLevel(stock, request.MinStockLevel);
                    LogTransaction(stock, request);
                    updatedStocks.Add((stock, sourceWarehouse, quantityBefore));
                    break;
                }
            case InventoryTransactionType.Out:
                {
                    var stock = await RequireStock(variant.Id, request.WarehouseId, cancellationToken).ConfigureAwait(false);
                    if (stock.Quantity < request.Quantity)
                    {
                        throw new ApplicationValidationException($"Insufficient stock. Available {stock.Quantity} and requested{request.Quantity}.");
                    }

                    var quantityBefore = stock.Quantity;
                    stock.Quantity -= request.Quantity;
                    UpdateMinStockLevel(stock, request.MinStockLevel);
                    LogTransaction(stock, request);
                    updatedStocks.Add((stock, sourceWarehouse, quantityBefore));
                    break;
                }
            case InventoryTransactionType.Adjust:
                {
                    var stock = await GetOrCreateStock(variant.Id, request.WarehouseId, cancellationToken).ConfigureAwait(false);
                    var quantityBefore = stock.Quantity;
                    stock.Quantity = request.Quantity;
                    UpdateMinStockLevel(stock, request.MinStockLevel);
                    LogTransaction(stock, request);
                    updatedStocks.Add((stock, sourceWarehouse, quantityBefore));
                    break;
                }
            case InventoryTransactionType.Move:
                {
                    if (!request.DestinationWarehouseId.HasValue)
                    {
                        throw new ApplicationValidationException("Destination warehouse is required for move transactions.");
                    }

                    if (request.DestinationWarehouseId.Value == request.WarehouseId)
                    {
                        throw new ApplicationValidationException("Source and destination warehouses must be different for move transactions.");
                    }

                    var stock = await RequireStock(variant.Id, request.WarehouseId, cancellationToken).ConfigureAwait(false);
                    if (stock.Quantity < request.Quantity)
                    {
                        throw new ApplicationValidationException($"Insufficient stock. Available {stock.Quantity} and requested{request.Quantity}.");
                    }

                    var destinationWarehouse = await context.Warehouses
                        .FirstOrDefaultAsync(w => w.Id == request.DestinationWarehouseId.Value, cancellationToken)
                        .ConfigureAwait(false);

                    if (destinationWarehouse is null)
                    {
                        throw new NotFoundException(nameof(Warehouse), request.DestinationWarehouseId.Value);
                    }

                    var destinationStock = await GetOrCreateStock(variant.Id, request.DestinationWarehouseId.Value, cancellationToken)
                        .ConfigureAwait(false);

                    var sourceBefore = stock.Quantity;
                    var destinationBefore = destinationStock.Quantity;

                    stock.Quantity -= request.Quantity;
                    destinationStock.Quantity += request.Quantity;

                    UpdateMinStockLevel(stock, request.MinStockLevel);
                    UpdateMinStockLevel(destinationStock, request.MinStockLevel);

                    LogTransaction(stock, request);

                    var destinationCommand = request with
                    {
                        WarehouseId = request.DestinationWarehouseId.Value,
                        DestinationWarehouseId = null
                    };

                    LogTransaction(destinationStock, destinationCommand);

                    updatedStocks.Add((stock, sourceWarehouse, sourceBefore));
                    updatedStocks.Add((destinationStock, destinationWarehouse, destinationBefore));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(request.TransactionType), request.TransactionType, "Unsupported transaction type.");
        }

        await RegisterManualOrderIfNeededAsync(variant, sourceWarehouse, request, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var adjustmentDetails = updatedStocks
            .Select(tuple => new InventoryAdjustmentDetail(
                tuple.stock.WarehouseId,
                tuple.warehouse.Name,
                tuple.quantityBefore,
                tuple.stock.Quantity))
            .ToList();

        await publisher.Publish(
            new InventoryAdjustedDomainEvent(
                variant.Id,
                variant.Sku,
                variant.Product?.Name ?? string.Empty,
                adjustmentDetails,
                request.TransactionType,
                request.Quantity,
                request.DestinationWarehouseId,
                request.ReferenceType,
                request.ReferenceId,
                request.Notes,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        var results = new List<InventoryStockDto>();
        foreach (var (stock, warehouse, _) in updatedStocks)
        {
            results.Add(await MapStockDto(stock, variant, warehouse, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    private async Task RegisterManualOrderIfNeededAsync(
        ProductVariant variant,
        Warehouse warehouse,
        AdjustInventoryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ReferenceType is not null || request.ReferenceId.HasValue)
        {
            return;
        }

        switch (request.TransactionType)
        {
            case InventoryTransactionType.In:
                await CreateManualPurchaseOrderAsync(variant, warehouse, request, cancellationToken).ConfigureAwait(false);
                break;
            case InventoryTransactionType.Out:
                await CreateManualSalesOrderAsync(variant, warehouse, request, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async Task CreateManualPurchaseOrderAsync(
        ProductVariant variant,
        Warehouse warehouse,
        AdjustInventoryCommand request,
        CancellationToken cancellationToken)
    {
        var supplier = await EnsureManualSupplierAsync(cancellationToken).ConfigureAwait(false);
        var currency = !string.IsNullOrWhiteSpace(variant.Product?.Currency)
            ? variant.Product!.Currency
            : "EUR";
        var unitPrice = variant.Price ?? variant.Product?.DefaultPrice ?? 0m;
        var lineTotal = request.Quantity * unitPrice;

        var order = new PurchaseOrder
        {
            Supplier = supplier,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Received,
            Currency = currency,
            Notes = BuildManualOrderNotes("Entrada manual de inventario", request.Notes, warehouse.Name),
            TotalAmount = lineTotal
        };

        order.Lines.Add(new PurchaseOrderLine
        {
            VariantId = variant.Id,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            Discount = null,
            TaxRateId = null,
            TotalLine = lineTotal
        });

        context.PurchaseOrders.Add(order);
    }

    private async Task CreateManualSalesOrderAsync(
        ProductVariant variant,
        Warehouse warehouse,
        AdjustInventoryCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await EnsureManualCustomerAsync(cancellationToken).ConfigureAwait(false);
        var currency = !string.IsNullOrWhiteSpace(variant.Product?.Currency)
            ? variant.Product!.Currency
            : "EUR";
        var unitPrice = variant.Price ?? variant.Product?.DefaultPrice ?? 0m;
        var lineTotal = request.Quantity * unitPrice;

        var order = new SalesOrder
        {
            Customer = customer,
            OrderDate = DateTime.UtcNow,
            Status = SalesOrderStatus.Delivered,
            Currency = currency,
            Notes = BuildManualOrderNotes("Salida manual de inventario", request.Notes, warehouse.Name),
            TotalAmount = lineTotal
        };

        order.Lines.Add(new SalesOrderLine
        {
            VariantId = variant.Id,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            Discount = null,
            TaxRateId = null,
            TotalLine = lineTotal
        });

        context.SalesOrders.Add(order);
    }

    private async Task<Supplier> EnsureManualSupplierAsync(CancellationToken cancellationToken)
    {
        var supplier = context.Suppliers.Local.FirstOrDefault(s => s.Name == ManualSupplierName)
            ?? await context.Suppliers
                .FirstOrDefaultAsync(s => s.Name == ManualSupplierName, cancellationToken)
                .ConfigureAwait(false);

        if (supplier is not null)
        {
            return supplier;
        }

        supplier = new Supplier
        {
            Name = ManualSupplierName,
            Notes = ManualPartyNotes
        };

        context.Suppliers.Add(supplier);
        return supplier;
    }

    private async Task<Customer> EnsureManualCustomerAsync(CancellationToken cancellationToken)
    {
        var customer = context.Customers.Local.FirstOrDefault(c => c.Name == ManualCustomerName)
            ?? await context.Customers
                .FirstOrDefaultAsync(c => c.Name == ManualCustomerName, cancellationToken)
                .ConfigureAwait(false);

        if (customer is not null)
        {
            return customer;
        }

        customer = new Customer
        {
            Name = ManualCustomerName,
            Notes = ManualPartyNotes
        };

        context.Customers.Add(customer);
        return customer;
    }

    private static string BuildManualOrderNotes(string prefix, string? notes, string warehouseName)
    {
        var baseNote = $"{prefix} en almacén {warehouseName}";

        if (!string.IsNullOrWhiteSpace(notes))
        {
            return $"{baseNote}. Notas: {notes.Trim()}";
        }

        return baseNote;
    }

    private async Task<InventoryStock> GetOrCreateStock(int variantId, int warehouseId, CancellationToken cancellationToken)
    {
        var stock = await context.InventoryStocks
            .FirstOrDefaultAsync(s => s.VariantId == variantId && s.WarehouseId == warehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (stock is null)
        {
            stock = new InventoryStock
            {
                VariantId = variantId,
                WarehouseId = warehouseId,
                Quantity = 0,
                ReservedQuantity = 0,
                MinStockLevel = 0
            };
            context.InventoryStocks.Add(stock);
        }

        return stock;
    }

    private async Task<InventoryStock> RequireStock(int variantId, int warehouseId, CancellationToken cancellationToken)
    {
        var stock = await context.InventoryStocks
            .FirstOrDefaultAsync(s => s.VariantId == variantId && s.WarehouseId == warehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (stock is null)
        {
            throw new NotFoundException(nameof(InventoryStock), $"Variant {variantId} in warehouse {warehouseId}");
        }

        return stock;
    }

    private void UpdateMinStockLevel(InventoryStock stock, decimal? minStockLevel)
    {
        if (minStockLevel.HasValue)
        {
            stock.MinStockLevel = minStockLevel.Value;
        }
    }

    private void LogTransaction(
        InventoryStock stock,
        AdjustInventoryCommand request)
    {
        var transaction = new InventoryTransaction
        {
            VariantId = stock.VariantId,
            WarehouseId = stock.WarehouseId,
            TransactionType = request.TransactionType,
            Quantity = request.Quantity,
            TransactionDate = DateTime.UtcNow,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            UserId = request.UserId,
            Notes = request.Notes
        };

        context.InventoryTransactions.Add(transaction);
    }

    private Task<InventoryStockDto> MapStockDto(
        InventoryStock stock,
        ProductVariant variant,
        Warehouse warehouse,
        CancellationToken cancellationToken)
    {
        var dto = new InventoryStockDto(
            stock.Id,
            stock.VariantId,
            stock.WarehouseId,
            stock.Quantity,
            stock.ReservedQuantity,
            stock.MinStockLevel,
            variant.Sku,
            variant.Product?.Name ?? string.Empty,
            warehouse.Name);
        return Task.FromResult(dto);
    }
}
