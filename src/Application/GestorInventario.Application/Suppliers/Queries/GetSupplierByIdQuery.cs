using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Suppliers.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Suppliers.Queries;

public record GetSupplierByIdQuery(int Id) : IRequest<SupplierDto>;

public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetSupplierByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<SupplierDto> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(supplier => supplier.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (supplier is null)
        {
            throw new NotFoundException(nameof(Supplier), request.Id);
        }

        return supplier.ToDto();
    }
}
