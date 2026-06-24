namespace AppInventory.Core.Entities;

public class Permission
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ResourceType { get; set; }
    public required string Action { get; set; }
    public ICollection<RolePermission> Roles { get; set; } = [];
}
