using FluentValidation;
using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Warehouses.Models;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Warehouses.Commands;

public record UpdateWarehouseCommand(int Id, string Name, string? Address, string? Description) : IRequest<WarehouseDto>;

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Address)
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(200);
    }
}

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, WarehouseDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateWarehouseCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<WarehouseDto> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (warehouse is null)
        {
            throw new NotFoundException(nameof(Warehouse), request.Id);
        }

        warehouse.Name = request.Name.Trim();
        warehouse.Address = request.Address?.Trim();
        warehouse.Description = request.Description?.Trim();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return warehouse.ToDto();
    }
}
