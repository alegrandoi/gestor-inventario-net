using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Customers.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Customers.Commands;

public record UpdateCustomerCommand(int Id, string Name, string? Email, string? Phone, string? Address, string? Notes) : IRequest<CustomerDto>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
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

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateCustomerCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (customer is null)
        {
            throw new NotFoundException(nameof(Customer), request.Id);
        }

        customer.Name = request.Name.Trim();
        customer.Email = request.Email?.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Address = request.Address?.Trim();
        customer.Notes = request.Notes?.Trim();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return customer.ToDto();
    }
}
