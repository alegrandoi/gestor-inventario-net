using GestorInventario.Application.Common.Exceptions;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Tenants.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GestorInventario.Application.Tenants.Commands;

public record UpdateTenantCommand(
    int Id,
    string Name,
    string Code,
    string? DefaultCulture,
    string? DefaultCurrency,
    bool IsActive,
    IReadOnlyCollection<UpdateBranchRequest> Branches) : IRequest<TenantDto>;

public record UpdateBranchRequest(
    int? Id,
    string Name,
    string Code,
    string? Locale,
    string? TimeZone,
    string? Currency,
    bool IsDefault,
    bool IsActive,
    bool Remove);

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    private readonly IGestorInventarioDbContext context;

    public UpdateTenantCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants
            .Include(t => t.Branches)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), request.Id);
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var exists = await context.Tenants
            .AnyAsync(t => t.Id != tenant.Id && t.Code == normalizedCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new InvalidOperationException($"Ya existe un inquilino con el código {normalizedCode}.");
        }

        tenant.Name = normalizedName;
        tenant.Code = normalizedCode;
        tenant.DefaultCulture = request.DefaultCulture?.Trim();
        tenant.DefaultCurrency = request.DefaultCurrency?.Trim();
        tenant.IsActive = request.IsActive;

        foreach (var branchUpdate in request.Branches)
        {
            if (branchUpdate.Id.HasValue)
            {
                var branch = tenant.Branches.FirstOrDefault(b => b.Id == branchUpdate.Id.Value);
                if (branch is null)
                {
                    throw new NotFoundException(nameof(Branch), branchUpdate.Id!.Value);
                }

                if (branchUpdate.Remove)
                {
                    tenant.Branches.Remove(branch);
                    continue;
                }

                branch.Name = branchUpdate.Name.Trim();
                branch.Code = branchUpdate.Code.Trim().ToUpperInvariant();
                branch.Locale = branchUpdate.Locale?.Trim();
                branch.TimeZone = branchUpdate.TimeZone?.Trim();
                branch.Currency = branchUpdate.Currency?.Trim();
                branch.IsDefault = branchUpdate.IsDefault;
                branch.IsActive = branchUpdate.IsActive;
            }
            else if (!branchUpdate.Remove)
            {
                tenant.Branches.Add(new Branch
                {
                    Name = branchUpdate.Name.Trim(),
                    Code = branchUpdate.Code.Trim().ToUpperInvariant(),
                    Locale = branchUpdate.Locale?.Trim(),
                    TimeZone = branchUpdate.TimeZone?.Trim(),
                    Currency = branchUpdate.Currency?.Trim(),
                    IsDefault = branchUpdate.IsDefault,
                    IsActive = branchUpdate.IsActive
                });
            }
        }

        if (!tenant.Branches.Any())
        {
            tenant.Branches.Add(new Branch
            {
                Name = "Sede principal",
                Code = "MAIN",
                IsDefault = true,
                IsActive = true
            });
        }

        if (!tenant.Branches.Any(b => b.IsDefault))
        {
            tenant.Branches.First().IsDefault = true;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
