using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Notifications;

internal sealed class NullNotificationProvider : INotificationProvider
{
    public bool IsAvailable => false;

    public Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
        => Task.CompletedTask;
}
