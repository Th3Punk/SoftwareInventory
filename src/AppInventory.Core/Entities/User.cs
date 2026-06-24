namespace AppInventory.Core.Entities;

public class User
{
    public int Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
