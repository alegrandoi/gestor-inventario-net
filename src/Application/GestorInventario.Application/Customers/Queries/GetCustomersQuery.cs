using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Customers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Customers.Queries;

public record GetCustomersQuery(string? SearchTerm) : IRequest<IReadOnlyCollection<CustomerDto>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, IReadOnlyCollection<CustomerDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetCustomersQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(customer =>
                customer.Name.Contains(term) ||
                (customer.Email != null && customer.Email.Contains(term)));
        }

        var customers = await query
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return customers
            .Select(customer => customer.ToDto())
            .ToList();
    }
}
