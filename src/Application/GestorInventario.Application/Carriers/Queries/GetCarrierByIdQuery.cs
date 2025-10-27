using GestorInventario.Application.Carriers.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Carriers.Queries;

public record GetCarrierByIdQuery(int Id) : IRequest<CarrierDto>;

public class GetCarrierByIdQueryHandler : IRequestHandler<GetCarrierByIdQuery, CarrierDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetCarrierByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CarrierDto> Handle(GetCarrierByIdQuery request, CancellationToken cancellationToken)
    {
        var carrier = await context.Carriers
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (carrier is null)
        {
            throw new NotFoundException(nameof(Carrier), request.Id);
        }

        return carrier.ToDto();
    }
}
