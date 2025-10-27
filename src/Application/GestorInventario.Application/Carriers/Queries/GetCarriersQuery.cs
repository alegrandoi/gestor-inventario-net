using System.Linq;
using GestorInventario.Application.Carriers.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Carriers.Queries;

public record GetCarriersQuery : IRequest<IReadOnlyCollection<CarrierDto>>;

public class GetCarriersQueryHandler : IRequestHandler<GetCarriersQuery, IReadOnlyCollection<CarrierDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetCarriersQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<CarrierDto>> Handle(GetCarriersQuery request, CancellationToken cancellationToken)
    {
        return await context.Carriers
            .AsNoTracking()
            .OrderBy(carrier => carrier.Name)
            .Select(carrier => carrier.ToDto())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
