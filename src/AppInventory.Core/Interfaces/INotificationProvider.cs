namespace AppInventory.Core.Interfaces;

public interface INotificationProvider
{
    bool IsAvailable { get; }
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}
