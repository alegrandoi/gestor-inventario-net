using System.Linq;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Commands.WarehouseProductVariants;

public record CreateWarehouseProductVariantAssignmentCommand(
    int WarehouseId,
    int VariantId,
    decimal? MinimumQuantity,
    decimal? TargetQuantity) : IRequest<WarehouseProductVariantAssignmentDto>;

public class CreateWarehouseProductVariantAssignmentCommandValidator : AbstractValidator<CreateWarehouseProductVariantAssignmentCommand>
{
    public CreateWarehouseProductVariantAssignmentCommandValidator()
    {
        RuleFor(command => command.WarehouseId)
            .GreaterThan(0);

        RuleFor(command => command.VariantId)
            .GreaterThan(0);

        RuleFor(command => command.MinimumQuantity)
            .GreaterThanOrEqualTo(0).When(command => command.MinimumQuantity.HasValue);

        RuleFor(command => command.TargetQuantity)
            .GreaterThanOrEqualTo(0).When(command => command.TargetQuantity.HasValue);

        RuleFor(command => command)
            .Must(command => !command.TargetQuantity.HasValue || !command.MinimumQuantity.HasValue || command.TargetQuantity >= command.MinimumQuantity)
            .WithMessage("La cantidad objetivo no puede ser inferior a la mínima definida.");
    }
}

public class CreateWarehouseProductVariantAssignmentCommandHandler : IRequestHandler<CreateWarehouseProductVariantAssignmentCommand, WarehouseProductVariantAssignmentDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateWarehouseProductVariantAssignmentCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<WarehouseProductVariantAssignmentDto> Handle(CreateWarehouseProductVariantAssignmentCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses
            .Include(w => w.WarehouseProductVariants)
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
        {
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);
        }

        var variant = await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken)
            .ConfigureAwait(false);

        if (variant is null)
        {
            throw new NotFoundException(nameof(ProductVariant), request.VariantId);
        }

        var assignmentExists = warehouse.WarehouseProductVariants
            .Any(existing => existing.VariantId == request.VariantId);

        if (assignmentExists)
        {
            throw new FluentValidation.ValidationException("El producto ya está vinculado a este almacén.");
        }

        var minimum = request.MinimumQuantity ?? 0m;
        var target = request.TargetQuantity ?? minimum;

        if (target < minimum)
        {
            throw new FluentValidation.ValidationException("La cantidad objetivo debe ser mayor o igual a la cantidad mínima.");
        }

        var assignment = new WarehouseProductVariant
        {
            WarehouseId = request.WarehouseId,
            VariantId = request.VariantId,
            MinimumQuantity = minimum,
            TargetQuantity = target
        };

        context.WarehouseProductVariants.Add(assignment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        assignment.Warehouse = warehouse;
        assignment.Variant = variant;

        return assignment.ToAssignmentDto(warehouse);
    }
}
