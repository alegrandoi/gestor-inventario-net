using System.Linq;
using FluentValidation;
using GestorInventario.Application.Categories.Models;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationValidationException = GestorInventario.Application.Common.Exceptions.ValidationException;

namespace GestorInventario.Application.Categories.Commands;

public record UpdateCategoryCommand(int Id, string Name, string? Description, int? ParentId) : IRequest<CategoryDto>;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(200);
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateCategoryCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == category.Id)
            {
                throw new ApplicationValidationException("La categoría no puede ser su propio padre.");
            }

            var parent = await context.Categories
                .AsNoTracking()
                .Where(candidate => candidate.Id == request.ParentId.Value)
                .Select(candidate => new { candidate.Id, candidate.ParentId })
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (parent is null)
            {
                throw new NotFoundException(nameof(Category), request.ParentId.Value);
            }

            var ancestorId = parent.ParentId;
            while (ancestorId.HasValue)
            {
                if (ancestorId.Value == category.Id)
                {
                    throw new ApplicationValidationException("No se puede asignar un descendiente como padre.");
                }

                ancestorId = await context.Categories
                    .Where(candidate => candidate.Id == ancestorId.Value)
                    .Select(candidate => candidate.ParentId)
                    .SingleOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var normalizedName = request.Name.Trim();
        var normalizedDescription = request.Description?.Trim();

        var duplicateExists = await context.Categories
            .AnyAsync(
                candidate => candidate.Id != request.Id
                    && candidate.ParentId == request.ParentId
                    && candidate.Name == normalizedName,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new ApplicationValidationException("Ya existe otra categoría con el mismo nombre en este nivel.");
        }

        category.Name = normalizedName;
        category.Description = normalizedDescription;
        category.ParentId = request.ParentId;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return category.ToDto();
    }
}
