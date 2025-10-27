using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.PriceLists.Commands;

public record DeletePriceListCommand(int Id) : IRequest;

public class DeletePriceListCommandHandler : IRequestHandler<DeletePriceListCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeletePriceListCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeletePriceListCommand request, CancellationToken cancellationToken)
    {
        var priceList = await context.PriceLists.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (priceList is null)
        {
            throw new NotFoundException(nameof(PriceList), request.Id);
        }

        context.PriceLists.Remove(priceList);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
