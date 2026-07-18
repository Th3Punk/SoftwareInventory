namespace AppInventory.Core.Entities;

public class DocumentationHistory
{
    public int Id { get; set; }
    public int DocumentationId { get; set; }
    public required string Content { get; set; }
    public int Version { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int? ArchivedByUserId { get; set; }
    public Documentation Documentation { get; set; } = null!;
}
