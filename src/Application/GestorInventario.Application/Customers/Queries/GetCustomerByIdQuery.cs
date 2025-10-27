using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Customers.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Customers.Queries;

public record GetCustomerByIdQuery(int Id) : IRequest<CustomerDto>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetCustomerByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (customer is null)
        {
            throw new NotFoundException(nameof(Customer), request.Id);
        }

        return customer.ToDto();
    }
}
