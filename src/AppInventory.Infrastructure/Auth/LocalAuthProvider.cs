using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppInventory.Infrastructure.Auth;

internal sealed class LocalAuthProvider : IAuthProvider
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<LocalAuthProvider> _logger;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutMinutes;

    public LocalAuthProvider(
        AppInventoryDbContext dbContext,
        IConfiguration configuration,
        ILogger<LocalAuthProvider> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<User>();
        _logger = logger;
        _maxFailedAttempts = configuration.GetValue("LocalAuth:MaxFailedAttempts", 5);
        _lockoutMinutes = configuration.GetValue("LocalAuth:LockoutMinutes", 15);
    }

    public string ProviderType => "Local";

    public async Task<AuthenticateResult> AuthenticateAsync(LoginRequest request, CancellationToken ct = default)
    {
        var identity = await _dbContext.ExternalIdentities
            .Include(e => e.User)
            .FirstOrDefaultAsync(e =>
                e.ProviderType == AuthProviderType.Local &&
                e.ExternalId == request.Username, ct);

        if (identity is null)
        {
            _logger.LogWarning("Login attempt for unknown username");
            return new AuthenticateResult(false, ErrorMessage: "Invalid username or password.");
        }

        var user = identity.User;

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user {UserId}", user.Id);
            return new AuthenticateResult(false, ErrorMessage: "Account is disabled.");
        }

        var credential = await _dbContext.LocalCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, ct);

        if (credential is null)
        {
            return new AuthenticateResult(false, ErrorMessage: "Invalid username or password.");
        }

        if (credential.LockedUntil.HasValue && credential.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked user {UserId}", user.Id);
            return new AuthenticateResult(false, ErrorMessage: "Account is temporarily locked. Please try again later.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, credential.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            credential.FailedAttempts++;

            if (credential.FailedAttempts >= _maxFailedAttempts)
            {
                credential.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutMinutes);
                _logger.LogWarning("User {UserId} locked out after {Attempts} failed attempts", user.Id, credential.FailedAttempts);
            }

            await _dbContext.SaveChangesAsync(ct);
            return new AuthenticateResult(false, ErrorMessage: "Invalid username or password.");
        }

        credential.FailedAttempts = 0;
        credential.LockedUntil = null;
        user.LastLogin = DateTime.UtcNow;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            credential.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            credential.PasswordUpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} authenticated successfully", user.Id);
        return new AuthenticateResult(true, UserId: user.Id, MustChangePassword: credential.MustChangePassword);
    }

    public async Task<ExternalIdentityDto?> GetExternalIdentityAsync(string externalId, CancellationToken ct = default)
    {
        var identity = await _dbContext.ExternalIdentities
            .Include(e => e.User)
            .FirstOrDefaultAsync(e =>
                e.ProviderType == AuthProviderType.Local &&
                e.ExternalId == externalId, ct);

        if (identity is null)
            return null;

        return new ExternalIdentityDto(
            AuthProviderType.Local,
            identity.ExternalId,
            identity.User.DisplayName,
            identity.User.Email);
    }
}
