using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ProductAttributes.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Queries;

public record GetProductAttributeGroupByIdQuery(int Id) : IRequest<ProductAttributeGroupDto>;

public class GetProductAttributeGroupByIdQueryHandler : IRequestHandler<GetProductAttributeGroupByIdQuery, ProductAttributeGroupDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetProductAttributeGroupByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ProductAttributeGroupDto> Handle(GetProductAttributeGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var group = await context.ProductAttributeGroups
            .Include(item => item.Values)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (group is null)
        {
            throw new NotFoundException(nameof(ProductAttributeGroup), request.Id);
        }

        return group.ToDto();
    }
}
