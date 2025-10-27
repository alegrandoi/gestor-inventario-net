using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.TaxRates.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.TaxRates.Queries;

public record GetTaxRateByIdQuery(int Id) : IRequest<TaxRateDto>;

public class GetTaxRateByIdQueryHandler : IRequestHandler<GetTaxRateByIdQuery, TaxRateDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetTaxRateByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<TaxRateDto> Handle(GetTaxRateByIdQuery request, CancellationToken cancellationToken)
    {
        var taxRate = await context.TaxRates
            .AsNoTracking()
            .FirstOrDefaultAsync(taxRate => taxRate.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (taxRate is null)
        {
            throw new NotFoundException(nameof(TaxRate), request.Id);
        }

        return taxRate.ToDto();
    }
}
