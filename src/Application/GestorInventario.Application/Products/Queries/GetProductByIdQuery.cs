using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Products.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Products.Queries;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetProductByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.TaxRate)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        return product.ToDto();
    }
}
