using GestorInventario.Application.Auditing.Events;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Products.Commands;

public record DeleteProductCommand(int Id) : IRequest;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IGestorInventarioDbContext context;
    private readonly IPublisher publisher;

    public DeleteProductCommandHandler(IGestorInventarioDbContext context, IPublisher publisher)
    {
        this.context = context;
        this.publisher = publisher;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await publisher.Publish(
            new ProductDeletedDomainEvent(
                product.Id,
                product.Code,
                product.Name,
                product.DefaultPrice,
                product.Currency,
                product.IsActive),
            cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
