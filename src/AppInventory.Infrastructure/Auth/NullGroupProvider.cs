using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Auth;

internal sealed class NullGroupProvider : IGroupProvider
{
    public Task<IReadOnlyList<string>> GetGroupsAsync(
        string externalUserId,
        AuthProviderType providerType,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
