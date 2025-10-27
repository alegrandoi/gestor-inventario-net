using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Tenants.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GestorInventario.Application.Tenants.Queries;

public record GetTenantByIdQuery(int Id) : IRequest<TenantDto>;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly IGestorInventarioDbContext context;

    public GetTenantByIdQueryHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants
            .Include(t => t.Branches)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), request.Id);
        }

        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Code,
            tenant.DefaultCulture,
            tenant.DefaultCurrency,
            tenant.IsActive,
            tenant.Branches
                .OrderByDescending(b => b.IsDefault)
                .ThenBy(b => b.Name)
                .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Locale, b.TimeZone, b.Currency, b.IsDefault, b.IsActive))
                .ToList());
    }
}
