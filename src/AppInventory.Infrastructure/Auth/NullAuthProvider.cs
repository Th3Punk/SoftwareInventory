using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Auth;

internal sealed class NullAuthProvider : IAuthProvider
{
    public string ProviderType => "Null";

    public Task<AuthenticateResult> AuthenticateAsync(LoginRequest request, CancellationToken ct = default)
        => Task.FromResult(new AuthenticateResult(false, ErrorMessage: "Authentication is not configured."));

    public Task<ExternalIdentityDto?> GetExternalIdentityAsync(string externalId, CancellationToken ct = default)
        => Task.FromResult<ExternalIdentityDto?>(null);
}
