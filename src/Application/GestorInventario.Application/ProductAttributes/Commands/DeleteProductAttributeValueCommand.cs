using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Commands;

public record DeleteProductAttributeValueCommand(int GroupId, int ValueId) : IRequest;

public class DeleteProductAttributeValueCommandValidator : AbstractValidator<DeleteProductAttributeValueCommand>
{
    public DeleteProductAttributeValueCommandValidator()
    {
        RuleFor(command => command.GroupId)
            .GreaterThan(0);

        RuleFor(command => command.ValueId)
            .GreaterThan(0);
    }
}

public class DeleteProductAttributeValueCommandHandler : IRequestHandler<DeleteProductAttributeValueCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteProductAttributeValueCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteProductAttributeValueCommand request, CancellationToken cancellationToken)
    {
        var value = await context.ProductAttributeValues
            .FirstOrDefaultAsync(item => item.Id == request.ValueId && item.GroupId == request.GroupId, cancellationToken)
            .ConfigureAwait(false);

        if (value is null)
        {
            throw new NotFoundException(nameof(ProductAttributeValue), request.ValueId);
        }

        context.ProductAttributeValues.Remove(value);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
