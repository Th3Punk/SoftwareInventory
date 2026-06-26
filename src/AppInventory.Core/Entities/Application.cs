namespace AppInventory.Core.Entities;

public class Application
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ShortDescription { get; set; }
    public string? DetailedDescription { get; set; }
    public ApplicationStatus Status { get; set; }
    public ApplicationType Type { get; set; }
    public required string OwnerTeam { get; set; }
    public SourceControlType SourceControl { get; set; } = SourceControlType.None;
    public string? RepositoryUrl { get; set; }
    public string? WikiUrl { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<ApplicationEnvironment> Environments { get; set; } = [];
    public ICollection<ApplicationContact> Contacts { get; set; } = [];
    public ICollection<ApplicationTag> Tags { get; set; } = [];
}

public enum ApplicationStatus { Active, Maintenance, Deprecated, Retired }

public enum ApplicationType { WebApp, ApiService, Library, BatchJob, MobileApp, Other }

public enum SourceControlType { None, Git, AzureDevOps }
