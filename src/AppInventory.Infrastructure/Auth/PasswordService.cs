using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AppInventory.Infrastructure.Auth;

public sealed class PasswordService
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly int _minLength;
    private readonly bool _requireDigit;
    private readonly bool _requireUppercase;
    private readonly bool _requireNonAlphanumeric;

    public PasswordService(AppInventoryDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<User>();

        var policy = configuration.GetSection("LocalAuth:PasswordPolicy");
        _minLength = policy.GetValue("MinLength", 12);
        _requireDigit = policy.GetValue("RequireDigit", true);
        _requireUppercase = policy.GetValue("RequireUppercase", true);
        _requireNonAlphanumeric = policy.GetValue("RequireNonAlphanumeric", true);
    }

    public string? ValidatePolicy(string password)
    {
        if (password.Length < _minLength)
            return $"Password must be at least {_minLength} characters long.";

        if (_requireDigit && !password.Any(char.IsDigit))
            return "Password must contain at least one digit.";

        if (_requireUppercase && !password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";

        if (_requireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            return "Password must contain at least one non-alphanumeric character.";

        return null;
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var credential = await _dbContext.LocalCredentials
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (credential is null)
            return (false, "User not found.");

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            credential.User, credential.PasswordHash, currentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
            return (false, "Current password is incorrect.");

        var policyError = ValidatePolicy(newPassword);
        if (policyError is not null)
            return (false, policyError);

        credential.PasswordHash = _passwordHasher.HashPassword(credential.User, newPassword);
        credential.MustChangePassword = false;
        credential.PasswordUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return (true, null);
    }
}
