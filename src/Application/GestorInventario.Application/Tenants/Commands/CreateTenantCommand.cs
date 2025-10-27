using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Tenants.Models;
using GestorInventario.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GestorInventario.Application.Tenants.Commands;

public record CreateTenantCommand(
    string Name,
    string Code,
    string? DefaultCulture,
    string? DefaultCurrency,
    bool IsActive,
    IReadOnlyCollection<CreateBranchRequest> Branches) : IRequest<TenantDto>;

public record CreateBranchRequest(
    string Name,
    string Code,
    string? Locale,
    string? TimeZone,
    string? Currency,
    bool IsDefault,
    bool IsActive);

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly IGestorInventarioDbContext context;

    public CreateTenantCommandHandler(IGestorInventarioDbContext context)
    {
        this.context = context;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var exists = await context.Tenants
            .AnyAsync(t => t.Code == normalizedCode, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new InvalidOperationException($"Ya existe un inquilino con el código {normalizedCode}.");
        }

        var tenant = new Tenant
        {
            Name = normalizedName,
            Code = normalizedCode,
            DefaultCulture = request.DefaultCulture?.Trim(),
            DefaultCurrency = request.DefaultCurrency?.Trim(),
            IsActive = request.IsActive
        };

        if (request.Branches.Count == 0)
        {
            tenant.Branches.Add(new Branch
            {
                Name = "Sede principal",
                Code = "MAIN",
                IsDefault = true,
                IsActive = true
            });
        }
        else
        {
            foreach (var branchRequest in request.Branches)
            {
                tenant.Branches.Add(new Branch
                {
                    Name = branchRequest.Name.Trim(),
                    Code = branchRequest.Code.Trim().ToUpperInvariant(),
                    Locale = branchRequest.Locale?.Trim(),
                    TimeZone = branchRequest.TimeZone?.Trim(),
                    Currency = branchRequest.Currency?.Trim(),
                    IsDefault = branchRequest.IsDefault,
                    IsActive = branchRequest.IsActive
                });
            }

            if (!tenant.Branches.Any(b => b.IsDefault))
            {
                tenant.Branches.First().IsDefault = true;
            }
        }

        await context.Tenants.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
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
