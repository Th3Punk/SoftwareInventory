namespace AppInventory.Core.Entities;

public class ApplicationContact
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public ContactType Type { get; set; }
    public required string Value { get; set; }
    public string? Label { get; set; }
    public Application Application { get; set; } = null!;
}

public enum ContactType { Email, Slack, Teams, SupportUrl, Phone }
