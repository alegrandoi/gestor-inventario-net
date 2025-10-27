using FluentValidation;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Customers.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Customers.Commands;

public record CreateCustomerCommand(string Name, string? Email, string? Phone, string? Address, string? Notes) : IRequest<CustomerDto>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
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

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateCustomerCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim()
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return customer.ToDto();
    }
}
