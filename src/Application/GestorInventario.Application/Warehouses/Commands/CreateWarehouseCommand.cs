using FluentValidation;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Warehouses.Commands;

public record CreateWarehouseCommand(string Name, string? Address, string? Description) : IRequest<WarehouseDto>;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Address)
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(200);
    }
}

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateWarehouseCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<WarehouseDto> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse
        {
            Name = request.Name.Trim(),
            Address = request.Address?.Trim(),
            Description = request.Description?.Trim()
        };

        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return warehouse.ToDto();
    }
}
