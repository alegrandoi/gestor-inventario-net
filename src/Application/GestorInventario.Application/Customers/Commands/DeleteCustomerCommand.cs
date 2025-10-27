using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Domain.Entities;
using MediatR;

namespace GestorInventario.Application.Customers.Commands;

public record DeleteCustomerCommand(int Id) : IRequest;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly IGestorInventarioDbContext context;

    public DeleteCustomerCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);

        if (customer is null)
        {
            throw new NotFoundException(nameof(Customer), request.Id);
        }

        context.Customers.Remove(customer);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
