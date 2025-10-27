using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ShippingRates.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ShippingRates.Queries;

public record GetShippingRatesQuery : IRequest<IReadOnlyCollection<ShippingRateDto>>;

public class GetShippingRatesQueryHandler : IRequestHandler<GetShippingRatesQuery, IReadOnlyCollection<ShippingRateDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetShippingRatesQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<ShippingRateDto>> Handle(GetShippingRatesQuery request, CancellationToken cancellationToken)
    {
        var shippingRates = await context.ShippingRates
            .AsNoTracking()
            .OrderBy(rate => rate.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return shippingRates
            .Select(rate => rate.ToDto())
            .ToList();
    }
}
