using AppInventory.Core.Entities;

namespace AppInventory.Core.Interfaces;

public interface IGroupProvider
{
    Task<IReadOnlyList<string>> GetGroupsAsync(
        string externalUserId,
        AuthProviderType providerType,
        CancellationToken ct = default);
}
