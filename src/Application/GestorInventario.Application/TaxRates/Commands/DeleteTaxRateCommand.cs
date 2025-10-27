using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.TaxRates.Commands;

public record DeleteTaxRateCommand(int Id) : IRequest;

public class DeleteTaxRateCommandHandler : IRequestHandler<DeleteTaxRateCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteTaxRateCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteTaxRateCommand request, CancellationToken cancellationToken)
    {
        var taxRate = await context.TaxRates.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (taxRate is null)
        {
            throw new NotFoundException(nameof(TaxRate), request.Id);
        }

        context.TaxRates.Remove(taxRate);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
