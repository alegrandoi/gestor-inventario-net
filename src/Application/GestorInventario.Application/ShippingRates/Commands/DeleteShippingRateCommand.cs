using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.ShippingRates.Commands;

public record DeleteShippingRateCommand(int Id) : IRequest;

public class DeleteShippingRateCommandHandler : IRequestHandler<DeleteShippingRateCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteShippingRateCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteShippingRateCommand request, CancellationToken cancellationToken)
    {
        var shippingRate = await context.ShippingRates.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (shippingRate is null)
        {
            throw new NotFoundException(nameof(ShippingRate), request.Id);
        }

        context.ShippingRates.Remove(shippingRate);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
