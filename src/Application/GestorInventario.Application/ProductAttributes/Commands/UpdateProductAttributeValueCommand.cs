using System;
using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.ProductAttributes.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.ProductAttributes.Commands;

public record UpdateProductAttributeValueCommand(
    int GroupId,
    int ValueId,
    string Name,
    string? Description,
    string? HexColor,
    int DisplayOrder,
    bool IsActive) : IRequest<ProductAttributeValueDto>;

public class UpdateProductAttributeValueCommandValidator : AbstractValidator<UpdateProductAttributeValueCommand>
{
    public UpdateProductAttributeValueCommandValidator()
    {
        RuleFor(command => command.GroupId)
            .GreaterThan(0);

        RuleFor(command => command.ValueId)
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

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductAttributeValueCommandHandler : IRequestHandler<UpdateProductAttributeValueCommand, ProductAttributeValueDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateProductAttributeValueCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<ProductAttributeValueDto> Handle(UpdateProductAttributeValueCommand request, CancellationToken cancellationToken)
    {
        var value = await context.ProductAttributeValues
            .FirstOrDefaultAsync(item => item.Id == request.ValueId && item.GroupId == request.GroupId, cancellationToken)
            .ConfigureAwait(false);

        if (value is null)
        {
            throw new NotFoundException(nameof(ProductAttributeValue), request.ValueId);
        }

        value.Name = request.Name.Trim();
        value.Description = request.Description?.Trim();
        value.HexColor = NormalizeColor(request.HexColor);
        value.DisplayOrder = request.DisplayOrder;
        value.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return value.ToDto();
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
