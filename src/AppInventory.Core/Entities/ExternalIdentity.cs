namespace AppInventory.Core.Entities;

public class ExternalIdentity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AuthProviderType ProviderType { get; set; }
    public required string ExternalId { get; set; }
    public string? ExternalGroupsJson { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public User User { get; set; } = null!;
}

public enum AuthProviderType { Local, ActiveDirectory, OAuth, Okta }
