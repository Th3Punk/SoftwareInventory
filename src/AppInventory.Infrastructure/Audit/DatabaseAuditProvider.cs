using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AppInventory.Infrastructure.Audit;

internal sealed class DatabaseAuditProvider : IAuditProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseAuditProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool IsAvailable => true;

    public async Task LogAsync(
        string action, string resourceType, string resourceId,
        int? userId = null, string? oldValueJson = null, string? newValueJson = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppInventoryDbContext>();

        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
    }
}
