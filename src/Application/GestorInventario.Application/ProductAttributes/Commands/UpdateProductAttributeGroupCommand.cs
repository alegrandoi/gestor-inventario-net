using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ProductAttributes.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Commands;

public record UpdateProductAttributeGroupCommand(
    int Id,
    string Name,
    string? Description,
    bool AllowCustomValues) : IRequest<ProductAttributeGroupDto>;

public class UpdateProductAttributeGroupCommandValidator : AbstractValidator<UpdateProductAttributeGroupCommand>
{
    public UpdateProductAttributeGroupCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(250);
    }
}

public class UpdateProductAttributeGroupCommandHandler : IRequestHandler<UpdateProductAttributeGroupCommand, ProductAttributeGroupDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateProductAttributeGroupCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ProductAttributeGroupDto> Handle(UpdateProductAttributeGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await context.ProductAttributeGroups
            .Include(item => item.Values)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (group is null)
        {
            throw new NotFoundException(nameof(ProductAttributeGroup), request.Id);
        }

        var normalizedName = request.Name.Trim();
        if (!string.Equals(group.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            group.Name = normalizedName;
            group.Slug = await GenerateUniqueSlugAsync(normalizedName, group.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            group.Name = normalizedName;
        }

        group.Description = request.Description?.Trim();
        group.AllowCustomValues = request.AllowCustomValues;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return group.ToDto();
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, int groupId, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        var candidate = baseSlug;
        var counter = 1;

        while (await context.ProductAttributeGroups
            .AnyAsync(group => group.Id != groupId && group.Slug == candidate, cancellationToken)
            .ConfigureAwait(false))
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
