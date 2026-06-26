namespace AppInventory.Core.Entities;

public class ApplicationTag
{
    public int ApplicationId { get; set; }
    public int TagId { get; set; }
    public Application Application { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
