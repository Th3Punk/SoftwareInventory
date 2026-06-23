namespace AppInventory.Core.Entities;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int? GrantedByUserId { get; set; }
    public DateTime GrantedAt { get; set; }
    public RoleGrantSource Source { get; set; }
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public enum RoleGrantSource { Manual, GroupMapping }
