using GestorInventario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorInventario.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(log => log.EntityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(log => log.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(log => log.Changes)
            .HasMaxLength(2000);

        builder.Property(log => log.TenantId)
            .IsRequired();

        builder.HasOne(log => log.User)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
