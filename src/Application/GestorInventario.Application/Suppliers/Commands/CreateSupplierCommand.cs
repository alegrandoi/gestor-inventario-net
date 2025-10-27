using FluentValidation;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Suppliers.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Suppliers.Commands;

public record CreateSupplierCommand(
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes) : IRequest<SupplierDto>;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.ContactName)
            .MaximumLength(150);

        RuleFor(command => command.Email)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.Phone)
            .MaximumLength(50);

        RuleFor(command => command.Address)
            .MaximumLength(200);

        RuleFor(command => command.Notes)
            .MaximumLength(200);
    }
}

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateSupplierCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim()
        };

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return supplier.ToDto();
    }
}
