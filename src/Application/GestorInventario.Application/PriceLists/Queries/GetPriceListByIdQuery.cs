using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.PriceLists.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.PriceLists.Queries;

public record GetPriceListByIdQuery(int Id) : IRequest<PriceListDto>;

public class GetPriceListByIdQueryHandler : IRequestHandler<GetPriceListByIdQuery, PriceListDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetPriceListByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<PriceListDto> Handle(GetPriceListByIdQuery request, CancellationToken cancellationToken)
    {
        var priceList = await context.PriceLists
            .AsNoTracking()
            .Include(list => list.ProductPrices)
                .ThenInclude(price => price.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(list => list.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (priceList is null)
        {
            throw new NotFoundException(nameof(PriceList), request.Id);
        }

        return priceList.ToDto();
    }
}
