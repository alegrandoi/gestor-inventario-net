using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PriceLists.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PriceLists.Queries;

public record GetPriceListsQuery : IRequest<IReadOnlyCollection<PriceListDto>>;

public class GetPriceListsQueryHandler : IRequestHandler<GetPriceListsQuery, IReadOnlyCollection<PriceListDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetPriceListsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<PriceListDto>> Handle(GetPriceListsQuery request, CancellationToken cancellationToken)
    {
        var priceLists = await context.PriceLists
            .AsNoTracking()
            .Include(list => list.ProductPrices)
                .ThenInclude(price => price.Variant)
                    .ThenInclude(variant => variant!.Product)
            .OrderBy(list => list.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return priceLists
            .Select(list => list.ToDto())
            .ToList();
    }
}
