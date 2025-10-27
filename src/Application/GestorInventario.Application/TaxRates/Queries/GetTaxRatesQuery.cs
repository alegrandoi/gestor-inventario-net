using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.TaxRates.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.TaxRates.Queries;

public record GetTaxRatesQuery : IRequest<IReadOnlyCollection<TaxRateDto>>;

public class GetTaxRatesQueryHandler : IRequestHandler<GetTaxRatesQuery, IReadOnlyCollection<TaxRateDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetTaxRatesQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<TaxRateDto>> Handle(GetTaxRatesQuery request, CancellationToken cancellationToken)
    {
        var taxRates = await context.TaxRates
            .AsNoTracking()
            .OrderBy(taxRate => taxRate.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return taxRates
            .Select(taxRate => taxRate.ToDto())
            .ToList();
    }
}
