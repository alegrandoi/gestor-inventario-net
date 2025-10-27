using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");

        builder.Property(transaction => transaction.Quantity)
            .HasPrecision(18, 4);

        builder.Property(transaction => transaction.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(transaction => transaction.ReferenceType)
            .HasMaxLength(20);

        builder.Property(transaction => transaction.Notes)
            .HasMaxLength(200);

        builder.HasIndex(transaction => transaction.TransactionDate);

        builder.HasIndex(transaction => new { transaction.TenantId, transaction.VariantId, transaction.WarehouseId });

        builder.HasIndex(transaction => transaction.ReferenceType);

        builder.HasOne(transaction => transaction.Variant)
            .WithMany(variant => variant.InventoryTransactions)
            .HasForeignKey(transaction => transaction.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(transaction => transaction.Warehouse)
            .WithMany(warehouse => warehouse.InventoryTransactions)
            .HasForeignKey(transaction => transaction.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(transaction => transaction.User)
            .WithMany(user => user.InventoryTransactions)
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(transaction => transaction.TenantId)
            .IsRequired();
    }
}
