using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Suppliers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Suppliers.Queries;

public record GetSuppliersQuery(string? SearchTerm) : IRequest<IReadOnlyCollection<SupplierDto>>;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, IReadOnlyCollection<SupplierDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetSuppliersQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(supplier =>
                supplier.Name.Contains(term) ||
                (supplier.Email != null && supplier.Email.Contains(term)));
        }

        var suppliers = await query
            .OrderBy(supplier => supplier.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return suppliers
            .Select(supplier => supplier.ToDto())
            .ToList();
    }
}
