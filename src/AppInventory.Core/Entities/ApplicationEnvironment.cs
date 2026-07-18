namespace AppInventory.Core.Entities;

public class ApplicationEnvironment
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public EnvironmentType Type { get; set; }
    public required string Url { get; set; }
    public string? Notes { get; set; }
    public bool IsPublic { get; set; } = true;
    public Application Application { get; set; } = null!;
}

public enum EnvironmentType { Development, Test, UAT, Production }
