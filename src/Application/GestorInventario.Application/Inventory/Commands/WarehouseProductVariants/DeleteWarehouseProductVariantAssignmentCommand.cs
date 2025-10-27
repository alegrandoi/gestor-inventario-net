using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Inventory.Commands.WarehouseProductVariants;

public record DeleteWarehouseProductVariantAssignmentCommand(int Id, int WarehouseId) : IRequest;

public class DeleteWarehouseProductVariantAssignmentCommandValidator : AbstractValidator<DeleteWarehouseProductVariantAssignmentCommand>
{
    public DeleteWarehouseProductVariantAssignmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.WarehouseId)
            .GreaterThan(0);
    }
}

public class DeleteWarehouseProductVariantAssignmentCommandHandler : IRequestHandler<DeleteWarehouseProductVariantAssignmentCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteWarehouseProductVariantAssignmentCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteWarehouseProductVariantAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await context.WarehouseProductVariants
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.WarehouseId == request.WarehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (assignment is null)
        {
            throw new NotFoundException(nameof(WarehouseProductVariant), request.Id);
        }

        context.WarehouseProductVariants.Remove(assignment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
