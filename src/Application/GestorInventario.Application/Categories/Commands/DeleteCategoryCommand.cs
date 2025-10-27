using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Categories.Commands;

public record DeleteCategoryCommand(int Id) : IRequest;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteCategoryCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
