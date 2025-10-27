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

public record CreateCategoryCommand(string Name, string? Description, int? ParentId) : IRequest<CategoryDto>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(200);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateCategoryCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue)
        {
            var parentExists = await context.Categories
                .AnyAsync(category => category.Id == request.ParentId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
            {
                throw new NotFoundException(nameof(Category), request.ParentId.Value);
            }
        }

        var normalizedName = request.Name.Trim();
        var normalizedDescription = request.Description?.Trim();

        var duplicateExists = await context.Categories
            .AnyAsync(
                category => category.Name == normalizedName
                    && category.ParentId == request.ParentId,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            throw new ApplicationValidationException("Ya existe una categoría con el mismo nombre en el nivel seleccionado.");
        }

        var category = new Category
        {
            Name = normalizedName,
            Description = normalizedDescription,
            ParentId = request.ParentId
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return category.ToDto();
    }
}
