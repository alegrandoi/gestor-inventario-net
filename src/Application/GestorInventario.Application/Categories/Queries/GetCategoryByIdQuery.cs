using System;
using System.Collections.Generic;
using System.Linq;
using GestorInventario.Application.Categories.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Categories.Queries;

public record GetCategoryByIdQuery(int Id) : IRequest<CategoryDto>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetCategoryByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var category = categories.FirstOrDefault(category => category.Id == request.Id);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        var childrenByParent = categories.ToLookup(candidate => candidate.ParentId);

        IReadOnlyCollection<CategoryDto> BuildHierarchy(int parentId)
        {
            return childrenByParent[parentId]
                .OrderBy(child => child.Name)
                .Select(child => child.ToDto(BuildHierarchy(child.Id)))
                .ToList();
        }

        return category.ToDto(BuildHierarchy(category.Id));
    }
}
