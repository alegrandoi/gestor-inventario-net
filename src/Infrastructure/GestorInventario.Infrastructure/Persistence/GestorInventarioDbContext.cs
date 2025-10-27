using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Domain.Abstractions;
using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace GestorInventario.Infrastructure.Persistence;

public class GestorInventarioDbContext : DbContext, IGestorInventarioDbContext
{
    private readonly ICurrentTenantService currentTenantService;
    private readonly IDataGovernancePolicyEnforcer dataGovernancePolicyEnforcer;

    public GestorInventarioDbContext(
        DbContextOptions<GestorInventarioDbContext> options,
        ICurrentTenantService currentTenantService,
        IDataGovernancePolicyEnforcer dataGovernancePolicyEnforcer)
        : base(options)
    {
        this.currentTenantService = currentTenantService;
        this.dataGovernancePolicyEnforcer = dataGovernancePolicyEnforcer;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductAttributeGroup> ProductAttributeGroups => Set<ProductAttributeGroup>();

    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<WarehouseProductVariant> WarehouseProductVariants => Set<WarehouseProductVariant>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Carrier> Carriers => Set<Carrier>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();

    public DbSet<SalesOrderAllocation> SalesOrderAllocations => Set<SalesOrderAllocation>();

    public DbSet<PriceList> PriceLists => Set<PriceList>();

    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<DemandHistory> DemandHistory => Set<DemandHistory>();

    public DbSet<DemandAggregate> DemandAggregates => Set<DemandAggregate>();

    public DbSet<SeasonalFactor> SeasonalFactors => Set<SeasonalFactor>();

    public DbSet<AbcPolicy> AbcPolicies => Set<AbcPolicy>();

    public DbSet<VariantAbcClassification> VariantAbcClassifications => Set<VariantAbcClassification>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();

    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.MarkCreated(DateTime.UtcNow);
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkUpdated(DateTime.UtcNow);
            }
        }

        await dataGovernancePolicyEnforcer.EnforceAsync(this, cancellationToken).ConfigureAwait(false);
        await ApplyTenantIdentifiersAsync(cancellationToken).ConfigureAwait(false);

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GestorInventarioDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private async Task ApplyTenantIdentifiersAsync(CancellationToken cancellationToken)
    {
        int? resolvedTenantId = null;

        foreach (var entry in ChangeTracker.Entries<ITenantScopedEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == 0)
            {
                resolvedTenantId ??= await ResolveTenantIdAsync(cancellationToken).ConfigureAwait(false);

                if (!resolvedTenantId.HasValue)
                {
                    throw new InvalidOperationException(
                        "No se pudo determinar el inquilino actual. Configura un inquilino activo o envía el encabezado 'X-Tenant-Id'.");
                }

                entry.Entity.TenantId = resolvedTenantId.Value;
            }
        }
    }

    private async Task<int?> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenantId = currentTenantService.TenantId;
        if (tenantId.HasValue && tenantId.Value > 0)
        {
            return tenantId.Value;
        }

        return await Tenants
            .AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.Id)
            .Select(tenant => (int?)tenant.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScopedEntity).IsAssignableFrom(entityType.ClrType) || typeof(Tenant).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var property = Expression.Property(parameter, nameof(ITenantScopedEntity.TenantId));

            var contextConstant = Expression.Constant(this);
            var tenantProperty = Expression.Property(contextConstant, nameof(CurrentTenantId));
            var hasTenant = Expression.Property(contextConstant, nameof(HasCurrentTenant));

            var getValueOrDefaultMethod = typeof(int?).GetMethod(nameof(Nullable<int>.GetValueOrDefault), Type.EmptyTypes)!;
            var tenantValue = Expression.Call(tenantProperty, getValueOrDefaultMethod);

            var equals = Expression.Equal(property, tenantValue);
            var legacyTenant = Expression.Equal(property, Expression.Constant(0));
            var tenantPredicate = Expression.OrElse(legacyTenant, equals);
            var body = Expression.Condition(hasTenant, tenantPredicate, Expression.Constant(true));

            var lambda = Expression.Lambda(body, parameter);

            entityType.SetQueryFilter(lambda);
        }
    }

    private int? CurrentTenantId => currentTenantService.TenantId;

    private bool HasCurrentTenant => currentTenantService.TenantId.HasValue && currentTenantService.TenantId.Value > 0;
}
