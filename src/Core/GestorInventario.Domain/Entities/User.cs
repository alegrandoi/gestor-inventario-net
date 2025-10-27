using GestorInventario.Domain.Abstractions;

namespace GestorInventario.Domain.Entities;

public class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsMfaEnabled { get; set; }

    public string? TotpSecret { get; set; }

    public string? TotpRecoveryCodes { get; set; }

    public string? PendingTwoFactorTokenHash { get; set; }

    public DateTime? PendingTwoFactorTokenExpiresAt { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
