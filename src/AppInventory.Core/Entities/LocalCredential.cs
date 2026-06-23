namespace AppInventory.Core.Entities;

public class LocalCredential
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string PasswordHash { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime PasswordUpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
