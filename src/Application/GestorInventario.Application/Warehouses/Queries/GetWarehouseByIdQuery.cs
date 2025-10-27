using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Warehouses.Queries;

public record GetWarehouseByIdQuery(int Id) : IRequest<WarehouseDto>;

public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetWarehouseByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.WarehouseProductVariants)
                .ThenInclude(assignment => assignment.Variant)
                    .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(warehouse => warehouse.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
        {
            throw new NotFoundException(nameof(Warehouse), request.Id);
        }

        return warehouse.ToDto();
    }
}
