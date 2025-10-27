using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ProductAttributes.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Commands;

public record CreateProductAttributeGroupCommand(
    string Name,
    string? Description,
    bool AllowCustomValues) : IRequest<ProductAttributeGroupDto>;

public class CreateProductAttributeGroupCommandValidator : AbstractValidator<CreateProductAttributeGroupCommand>
{
    public CreateProductAttributeGroupCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(250);
    }
}

public class CreateProductAttributeGroupCommandHandler : IRequestHandler<CreateProductAttributeGroupCommand, ProductAttributeGroupDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateProductAttributeGroupCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ProductAttributeGroupDto> Handle(CreateProductAttributeGroupCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var slug = await GenerateUniqueSlugAsync(normalizedName, cancellationToken).ConfigureAwait(false);

        var group = new ProductAttributeGroup
        {
            Name = normalizedName,
            Slug = slug,
            Description = request.Description?.Trim(),
            AllowCustomValues = request.AllowCustomValues
        };

        context.ProductAttributeGroups.Add(group);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return group.ToDto();
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        var candidate = baseSlug;
        var counter = 1;

        while (await context.ProductAttributeGroups.AnyAsync(group => group.Slug == candidate, cancellationToken).ConfigureAwait(false))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }

    private static string Slugify(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                builder.Append('-');
            }
        }

        var slug = Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
        return slug.Length > 120 ? slug[..120] : slug;
    }
}
