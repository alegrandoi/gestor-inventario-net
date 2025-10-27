using System;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ProductAttributes.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GestorInventario.Application.ProductAttributes.Commands;

public record CreateProductAttributeValueCommand(
    int GroupId,
    string Name,
    string? Description,
    string? HexColor,
    int? DisplayOrder,
    bool IsActive) : IRequest<ProductAttributeValueDto>;

public class CreateProductAttributeValueCommandValidator : AbstractValidator<CreateProductAttributeValueCommand>
{
    public CreateProductAttributeValueCommandValidator()
    {
        RuleFor(command => command.GroupId)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Description)
            .MaximumLength(200);

        RuleFor(command => command.HexColor)
            .Matches("^#?[0-9A-Fa-f]{6}$")
            .When(command => !string.IsNullOrWhiteSpace(command.HexColor))
            .WithMessage("El color debe tener el formato hexadecimal #RRGGBB.");
    }
}

public class CreateProductAttributeValueCommandHandler : IRequestHandler<CreateProductAttributeValueCommand, ProductAttributeValueDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateProductAttributeValueCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ProductAttributeValueDto> Handle(CreateProductAttributeValueCommand request, CancellationToken cancellationToken)
    {
        var group = await context.ProductAttributeGroups
            .FirstOrDefaultAsync(item => item.Id == request.GroupId, cancellationToken)
            .ConfigureAwait(false);

        if (group is null)
        {
            throw new NotFoundException(nameof(ProductAttributeGroup), request.GroupId);
        }

        var displayOrder = await DetermineDisplayOrderAsync(request, cancellationToken).ConfigureAwait(false);

        var value = new ProductAttributeValue
        {
            GroupId = request.GroupId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            HexColor = NormalizeColor(request.HexColor),
            DisplayOrder = displayOrder,
            IsActive = request.IsActive
        };

        context.ProductAttributeValues.Add(value);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return value.ToDto();
    }

    private async Task<int> DetermineDisplayOrderAsync(CreateProductAttributeValueCommand request, CancellationToken cancellationToken)
    {
        if (request.DisplayOrder.HasValue)
        {
            var normalizedOrder = request.DisplayOrder.Value;

            var valuesToShift = await context.ProductAttributeValues
                .Where(value => value.GroupId == request.GroupId && value.DisplayOrder >= normalizedOrder)
                .OrderBy(value => value.DisplayOrder)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var existing in valuesToShift)
            {
                existing.DisplayOrder++;
            }

            return normalizedOrder;
        }

        var maxDisplayOrder = await context.ProductAttributeValues
            .Where(value => value.GroupId == request.GroupId)
            .MaxAsync(value => (int?)value.DisplayOrder, cancellationToken)
            .ConfigureAwait(false) ?? -1;

        return maxDisplayOrder + 1;
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        return color.StartsWith("#", StringComparison.Ordinal)
            ? color.ToUpperInvariant()
            : $"#{color.ToUpperInvariant()}";
    }
}
