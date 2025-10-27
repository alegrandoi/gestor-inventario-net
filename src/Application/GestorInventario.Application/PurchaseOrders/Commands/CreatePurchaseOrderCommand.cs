using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PurchaseOrders.Models;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PurchaseOrders.Commands;

public record CreatePurchaseOrderCommand(
    int SupplierId,
    DateTime OrderDate,
    PurchaseOrderStatus Status,
    string Currency,
    string? Notes,
    IReadOnlyCollection<CreatePurchaseOrderLineRequest> Lines) : IRequest<PurchaseOrderDto>;

public record CreatePurchaseOrderLineRequest(int VariantId, decimal Quantity, decimal UnitPrice, decimal? Discount, int? TaxRateId);

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(command => command.SupplierId)
            .GreaterThan(0);

        RuleFor(command => command.OrderDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));

        RuleFor(command => command.Currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(command => command.Notes)
            .MaximumLength(200);

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

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreatePurchaseOrderCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers.FindAsync([request.SupplierId], cancellationToken).ConfigureAwait(false);

        if (supplier is null)
        {
            throw new NotFoundException(nameof(Supplier), request.SupplierId);
        }

        var order = new PurchaseOrder
        {
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate,
            Status = request.Status,
            Currency = request.Currency.Trim(),
            Notes = request.Notes?.Trim()
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

            order.Lines.Add(new PurchaseOrderLine
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount,
                TaxRateId = line.TaxRateId,
                TotalLine = totalLine
            });
        }

        order.TotalAmount = totalAmount;

        context.PurchaseOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        order = await context.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Lines)
                .ThenInclude(line => line.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstAsync(o => o.Id == order.Id, cancellationToken)
            .ConfigureAwait(false);

        return order.ToDto();
    }
}
