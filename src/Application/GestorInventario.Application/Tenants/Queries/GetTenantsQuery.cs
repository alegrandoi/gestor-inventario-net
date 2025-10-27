using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Tenants.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GestorInventario.Application.Tenants.Queries;

public record GetTenantsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyCollection<TenantDto>>;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IReadOnlyCollection<TenantDto>>
{
    private readonly IGestorInventarioDbContext context;

    public GetTenantsQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> tenantsQuery = context.Tenants
            .AsNoTracking()
            .Include(t => t.Branches);

        if (!request.IncludeInactive)
        {
            tenantsQuery = tenantsQuery.Where(t => t.IsActive);
        }

        var tenants = await tenantsQuery
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tenants
            .Select(t => new TenantDto(
                t.Id,
                t.Name,
                t.Code,
                t.DefaultCulture,
                t.DefaultCurrency,
                t.IsActive,
                t.Branches
                    .OrderByDescending(b => b.IsDefault)
                    .ThenBy(b => b.Name)
                    .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Locale, b.TimeZone, b.Currency, b.IsDefault, b.IsActive))
                    .ToList()))
            .ToList();
    }
}
