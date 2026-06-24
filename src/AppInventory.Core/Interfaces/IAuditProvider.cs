namespace AppInventory.Core.Interfaces;

public interface IAuditProvider
{
    bool IsAvailable { get; }
    Task LogAsync(string action, string resourceType, string resourceId,
        int? userId = null, string? oldValueJson = null, string? newValueJson = null,
        string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
}
