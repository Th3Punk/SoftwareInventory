namespace AppInventory.Core.Entities;

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public ICollection<ApplicationTag> Applications { get; set; } = [];
}
