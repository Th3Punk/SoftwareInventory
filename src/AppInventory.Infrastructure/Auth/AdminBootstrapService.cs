using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppInventory.Infrastructure.Auth;

internal sealed class AdminBootstrapService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminBootstrapService> _logger;

    public AdminBootstrapService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AdminBootstrapService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppInventoryDbContext>();

        var adminExists = await dbContext.ExternalIdentities
            .AnyAsync(e => e.ProviderType == AuthProviderType.Local && e.ExternalId == "admin", ct);

        if (adminExists)
            return;

        var initialPassword = _configuration["ADMIN_INITIAL_PASSWORD"]
            ?? Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD");

        if (string.IsNullOrWhiteSpace(initialPassword))
        {
            _logger.LogWarning("ADMIN_INITIAL_PASSWORD not set — skipping bootstrap admin creation");
            return;
        }

        var user = new User
        {
            DisplayName = "Administrator",
            Email = "admin@localhost",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);

        var identity = new ExternalIdentity
        {
            UserId = user.Id,
            ProviderType = AuthProviderType.Local,
            ExternalId = "admin",
            LastSyncedAt = DateTime.UtcNow
        };
        dbContext.ExternalIdentities.Add(identity);

        var hasher = new PasswordHasher<User>();
        var credential = new LocalCredential
        {
            UserId = user.Id,
            PasswordHash = hasher.HashPassword(user, initialPassword),
            MustChangePassword = true,
            PasswordUpdatedAt = DateTime.UtcNow
        };
        dbContext.LocalCredentials.Add(credential);

        var adminRole = new UserRole
        {
            UserId = user.Id,
            RoleId = 1,
            GrantedAt = DateTime.UtcNow,
            Source = RoleGrantSource.Manual
        };
        dbContext.UserRoles.Add(adminRole);

        await dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Bootstrap admin user created with MustChangePassword=true");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
