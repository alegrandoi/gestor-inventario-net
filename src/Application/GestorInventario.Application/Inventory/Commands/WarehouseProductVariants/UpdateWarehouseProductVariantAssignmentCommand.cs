using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Inventory.Models;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Commands.WarehouseProductVariants;

public record UpdateWarehouseProductVariantAssignmentCommand(
    int Id,
    int WarehouseId,
    decimal MinimumQuantity,
    decimal TargetQuantity) : IRequest<WarehouseProductVariantAssignmentDto>;

public class UpdateWarehouseProductVariantAssignmentCommandValidator : AbstractValidator<UpdateWarehouseProductVariantAssignmentCommand>
{
    public UpdateWarehouseProductVariantAssignmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.WarehouseId)
            .GreaterThan(0);

        RuleFor(command => command.MinimumQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.TargetQuantity)
            .GreaterThanOrEqualTo(command => command.MinimumQuantity)
            .WithMessage("La cantidad objetivo debe ser mayor o igual a la cantidad mínima.");
    }
}

public class UpdateWarehouseProductVariantAssignmentCommandHandler : IRequestHandler<UpdateWarehouseProductVariantAssignmentCommand, WarehouseProductVariantAssignmentDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateWarehouseProductVariantAssignmentCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<WarehouseProductVariantAssignmentDto> Handle(UpdateWarehouseProductVariantAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await context.WarehouseProductVariants
            .Include(a => a.Warehouse)
            .Include(a => a.Variant)!
                .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.WarehouseId == request.WarehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (assignment is null)
        {
            throw new NotFoundException(nameof(WarehouseProductVariant), request.Id);
        }

        assignment.MinimumQuantity = request.MinimumQuantity;
        assignment.TargetQuantity = request.TargetQuantity;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return assignment.ToAssignmentDto(assignment.Warehouse);
    }
}
