namespace AppInventory.Core.Entities;

public class Documentation
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DocumentationType Type { get; set; }
    public DocumentationStatus Status { get; set; } = DocumentationStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? AuthorUserId { get; set; }
    public Application Application { get; set; } = null!;
    public User? Author { get; set; }
    public ICollection<DocumentationHistory> History { get; set; } = [];
}

public enum DocumentationType { User, Developer, Operations }

public enum DocumentationStatus { Draft, Published, Archived }
