using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Commands;

public record DeleteProductAttributeGroupCommand(int Id) : IRequest;

public class DeleteProductAttributeGroupCommandValidator : AbstractValidator<DeleteProductAttributeGroupCommand>
{
    public DeleteProductAttributeGroupCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);
    }
}

public class DeleteProductAttributeGroupCommandHandler : IRequestHandler<DeleteProductAttributeGroupCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteProductAttributeGroupCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteProductAttributeGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await context.ProductAttributeGroups
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (group is null)
        {
            throw new NotFoundException(nameof(ProductAttributeGroup), request.Id);
        }

        context.ProductAttributeGroups.Remove(group);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
