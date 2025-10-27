using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Suppliers.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Suppliers.Commands;

public record UpdateSupplierCommand(
    int Id,
    string Name,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes) : IRequest<SupplierDto>;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

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

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateSupplierCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<SupplierDto> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (supplier is null)
        {
            throw new NotFoundException(nameof(Supplier), request.Id);
        }

        supplier.Name = request.Name.Trim();
        supplier.ContactName = request.ContactName?.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.Phone = request.Phone?.Trim();
        supplier.Address = request.Address?.Trim();
        supplier.Notes = request.Notes?.Trim();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return supplier.ToDto();
    }
}
