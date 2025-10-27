using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ShippingRates.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ShippingRates.Queries;

public record GetShippingRateByIdQuery(int Id) : IRequest<ShippingRateDto>;

public class GetShippingRateByIdQueryHandler : IRequestHandler<GetShippingRateByIdQuery, ShippingRateDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetShippingRateByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ShippingRateDto> Handle(GetShippingRateByIdQuery request, CancellationToken cancellationToken)
    {
        var shippingRate = await context.ShippingRates
            .AsNoTracking()
            .FirstOrDefaultAsync(rate => rate.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (shippingRate is null)
        {
            throw new NotFoundException(nameof(ShippingRate), request.Id);
        }

        return shippingRate.ToDto();
    }
}
