namespace AppInventory.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public ICollection<RolePermission> Permissions { get; set; } = [];
    public ICollection<UserRole> UserAssignments { get; set; } = [];
    public ICollection<GroupRoleMapping> GroupMappings { get; set; } = [];
}
