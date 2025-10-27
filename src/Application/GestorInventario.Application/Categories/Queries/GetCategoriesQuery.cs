using System;
using System.Collections.Generic;
using System.Linq;
using GestorInventario.Application.Categories.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Categories.Queries;

public record GetCategoriesQuery : IRequest<IReadOnlyCollection<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetCategoriesQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (categories.Count == 0)
        {
            return Array.Empty<CategoryDto>();
        }

        var childrenByParent = categories.ToLookup(category => category.ParentId);

        IReadOnlyCollection<CategoryDto> BuildHierarchy(int? parentId)
        {
            return childrenByParent[parentId]
                .OrderBy(child => child.Name)
                .Select(child => child.ToDto(BuildHierarchy(child.Id)))
                .ToList();
        }

        return BuildHierarchy(parentId: null);
    }
}
