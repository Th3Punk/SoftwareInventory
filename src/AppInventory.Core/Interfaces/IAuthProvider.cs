using AppInventory.Core.Entities;

namespace AppInventory.Core.Interfaces;

public interface IAuthProvider
{
    string ProviderType { get; }
    Task<AuthenticateResult> AuthenticateAsync(LoginRequest request, CancellationToken ct = default);
    Task<ExternalIdentityDto?> GetExternalIdentityAsync(string externalId, CancellationToken ct = default);
}

public record LoginRequest(string Username, string Password);
public record AuthenticateResult(bool Success, int? UserId = null, string? ErrorMessage = null, bool MustChangePassword = false);

public record ExternalIdentityDto(
    AuthProviderType ProviderType,
    string ExternalId,
    string DisplayName,
    string Email);
