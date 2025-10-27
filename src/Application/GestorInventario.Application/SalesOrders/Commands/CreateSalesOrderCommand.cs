using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.SalesOrders.Events;
using GestorInventario.Application.SalesOrders.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.SalesOrders.Commands;

public record CreateSalesOrderCommand(
    int CustomerId,
    DateTime OrderDate,
    SalesOrderStatus Status,
    string? ShippingAddress,
    string Currency,
    string? Notes,
    int? CarrierId,
    DateTime? EstimatedDeliveryDate,
    IReadOnlyCollection<CreateSalesOrderLineRequest> Lines) : IRequest<SalesOrderDto>;

public record CreateSalesOrderLineRequest(int VariantId, decimal Quantity, decimal UnitPrice, decimal? Discount, int? TaxRateId);

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .GreaterThan(0);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(command => command.ShippingAddress)
            .MaximumLength(200);

        RuleFor(command => command.Notes)
            .MaximumLength(200);

        RuleFor(command => command.CarrierId)
            .GreaterThan(0)
            .When(command => command.CarrierId.HasValue);

        RuleFor(command => command.EstimatedDeliveryDate)
            .GreaterThanOrEqualTo(command => command.OrderDate.Date)
            .When(command => command.EstimatedDeliveryDate.HasValue);

        RuleFor(command => command.Lines)
            .NotEmpty();

        RuleForEach(command => command.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.VariantId)
                    .GreaterThan(0);

                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0);

                line.RuleFor(l => l.UnitPrice)
                    .GreaterThanOrEqualTo(0);

                line.RuleFor(l => l.Discount)
                    .GreaterThanOrEqualTo(0)
                    .When(l => l.Discount.HasValue);
            });
    }
}

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, SalesOrderDto>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public CreateSalesOrderCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<SalesOrderDto> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FindAsync([request.CustomerId], cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            throw new NotFoundException(nameof(Customer), request.CustomerId);
        }

        if (request.CarrierId.HasValue)
        {
            var carrierExists = await context.Carriers
                .AnyAsync(carrier => carrier.Id == request.CarrierId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!carrierExists)
            {
                throw new NotFoundException(nameof(Carrier), request.CarrierId.Value);
            }
        }

        var orderStatus = Enum.IsDefined(typeof(SalesOrderStatus), request.Status)
            ? request.Status
            : SalesOrderStatus.Pending;

        var order = new SalesOrder
        {
            CustomerId = request.CustomerId,
            OrderDate = request.OrderDate,
            Status = orderStatus,
            ShippingAddress = request.ShippingAddress?.Trim(),
            Currency = request.Currency.Trim(),
            Notes = request.Notes?.Trim(),
            CarrierId = request.CarrierId,
            EstimatedDeliveryDate = request.EstimatedDeliveryDate
        };

        decimal totalAmount = 0;

        foreach (var line in request.Lines)
        {
            var variant = await context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == line.VariantId, cancellationToken)
                .ConfigureAwait(false);

            if (variant is null)
            {
                throw new NotFoundException(nameof(ProductVariant), line.VariantId);
            }

            decimal discount = line.Discount ?? 0;
            decimal subtotal = line.Quantity * line.UnitPrice - discount;
            decimal taxAmount = 0;

            if (line.TaxRateId.HasValue)
            {
                var taxRate = await context.TaxRates.FindAsync([line.TaxRateId.Value], cancellationToken).ConfigureAwait(false);
                if (taxRate is null)
                {
                    throw new NotFoundException(nameof(TaxRate), line.TaxRateId.Value);
                }

                taxAmount = subtotal * (taxRate.Rate / 100m);
            }

            var totalLine = subtotal + taxAmount;
            totalAmount += totalLine;

            var orderLine = new SalesOrderLine
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount,
                TaxRateId = line.TaxRateId,
                TotalLine = totalLine,
                Variant = variant
            };

            await ReserveStockForLineAsync(orderLine, cancellationToken).ConfigureAwait(false);

            order.Lines.Add(orderLine);
        }

        order.TotalAmount = totalAmount;

        context.SalesOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        order = await context.SalesOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Carrier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(o => o.Shipments)
                .ThenInclude(shipment => shipment.Warehouse)
            .FirstAsync(o => o.Id == order.Id, cancellationToken)
            .ConfigureAwait(false);

        var orderDto = order.ToDto();

        await publisher.Publish(
            new SalesOrderCreatedDomainEvent(
                orderDto.Id,
                orderDto.Status,
                orderDto.CustomerName,
                orderDto.TotalAmount,
                orderDto.OrderDate),
            cancellationToken).ConfigureAwait(false);

        return orderDto;
    }

    private async Task ReserveStockForLineAsync(SalesOrderLine line, CancellationToken cancellationToken)
    {
        var stocks = await context.InventoryStocks
            .Include(stock => stock.Warehouse)
            .Where(stock => stock.VariantId == line.VariantId)
            .OrderByDescending(stock => (double)(stock.Quantity - stock.ReservedQuantity))
            .ThenBy(stock => stock.WarehouseId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var remainingQuantity = line.Quantity;

        foreach (var stock in stocks)
        {
            var available = stock.Quantity - stock.ReservedQuantity;
            if (available <= 0)
            {
                continue;
            }

            var allocationQuantity = Math.Min(remainingQuantity, available);
            stock.ReservedQuantity += allocationQuantity;

            var allocation = new SalesOrderAllocation
            {
                WarehouseId = stock.WarehouseId,
                Warehouse = stock.Warehouse,
                Quantity = allocationQuantity,
                FulfilledQuantity = 0,
                Status = SalesOrderAllocationStatus.Reserved,
                SalesOrderLine = line
            };

            line.Allocations.Add(allocation);

            remainingQuantity -= allocationQuantity;

            if (remainingQuantity <= 0)
            {
                break;
            }
        }

        if (remainingQuantity > 0)
        {
            throw new ApplicationValidationException($"Insufficient stock for variant {line.Variant?.Sku ?? line.VariantId.ToString()}. Missing {remainingQuantity} units.");
        }
    }
}
