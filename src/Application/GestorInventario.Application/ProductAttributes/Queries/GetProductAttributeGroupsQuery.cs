using System.Linq;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ProductAttributes.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Queries;

public record GetProductAttributeGroupsQuery : IRequest<IReadOnlyCollection<ProductAttributeGroupDto>>;

public class GetProductAttributeGroupsQueryHandler : IRequestHandler<GetProductAttributeGroupsQuery, IReadOnlyCollection<ProductAttributeGroupDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetProductAttributeGroupsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<ProductAttributeGroupDto>> Handle(GetProductAttributeGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await context.ProductAttributeGroups
            .Include(group => group.Values)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return groups
            .Select(group => group.ToDto())
            .ToArray();
    }
}
