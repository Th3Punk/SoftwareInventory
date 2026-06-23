using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Audit;

internal sealed class NullAuditProvider : IAuditProvider
{
    public bool IsAvailable => false;

    public Task LogAsync(string action, string resourceType, string resourceId,
        int? userId = null, string? oldValue = null, string? newValue = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default)
        => Task.CompletedTask;
}
