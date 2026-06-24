namespace AppInventory.Core.Entities;

public class GroupRoleMapping
{
    public int Id { get; set; }
    public AuthProviderType ProviderType { get; set; }
    public required string ExternalGroupRef { get; set; }
    public int RoleId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Role Role { get; set; } = null!;
}
