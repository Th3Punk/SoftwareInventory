using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AppInventory.Core.Authorization;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppInventory.Api.Middleware;

public static class CookieSessionDefaults
{
    public const string AuthenticationScheme = "CookieSession";
}

public class CookieSessionAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly string _cookieName;

    public CookieSessionAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppInventoryDbContext dbContext,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _cookieName = configuration.GetValue("LocalAuth:SessionCookieName", "appinventory.auth")!;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(_cookieName, out var sessionValue) ||
            string.IsNullOrEmpty(sessionValue))
        {
            return AuthenticateResult.NoResult();
        }

        if (!int.TryParse(sessionValue, out var userId))
        {
            return AuthenticateResult.Fail("Invalid session.");
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.ExternalIdentities)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return AuthenticateResult.Fail("User not found.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimNames.IsActive, user.IsActive.ToString().ToLowerInvariant())
        };

        var roleNames = new HashSet<string>(
            user.UserRoles.Select(ur => ur.Role.Name));

        var groupRoles = await ResolveGroupRoleMappingsAsync(user.ExternalIdentities);
        foreach (var roleName in groupRoles)
        {
            roleNames.Add(roleName);
        }

        foreach (var roleName in roleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var credential = await _dbContext.LocalCredentials
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (credential?.MustChangePassword == true)
        {
            claims.Add(new Claim(ClaimNames.MustChangePassword, "true"));
        }

        var identity = new ClaimsIdentity(claims, CookieSessionDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, CookieSessionDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<IReadOnlyList<string>> ResolveGroupRoleMappingsAsync(
        ICollection<Core.Entities.ExternalIdentity> identities)
    {
        if (identities.Count == 0)
            return [];

        var allGroups = new List<(Core.Entities.AuthProviderType providerType, string group)>();

        foreach (var identity in identities)
        {
            if (string.IsNullOrEmpty(identity.ExternalGroupsJson))
                continue;

            try
            {
                var groups = JsonSerializer.Deserialize<List<string>>(identity.ExternalGroupsJson);
                if (groups is not null)
                {
                    allGroups.AddRange(groups.Select(g => (identity.ProviderType, g)));
                }
            }
            catch (JsonException)
            {
            }
        }

        if (allGroups.Count == 0)
            return [];

        var providerTypes = allGroups.Select(g => g.providerType).Distinct().ToList();
        var groupRefs = allGroups.Select(g => g.group).Distinct().ToList();

        var mappings = await _dbContext.GroupRoleMappings
            .Include(m => m.Role)
            .Where(m => m.IsActive &&
                        providerTypes.Contains(m.ProviderType) &&
                        groupRefs.Contains(m.ExternalGroupRef))
            .ToListAsync();

        return mappings
            .Where(m => allGroups.Any(g =>
                g.providerType == m.ProviderType && g.group == m.ExternalGroupRef))
            .Select(m => m.Role.Name)
            .Distinct()
            .ToList();
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Unauthorized",
            status = 401,
            detail = "Authentication is required."
        };
        await Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Forbidden",
            status = 403,
            detail = "You do not have permission to access this resource."
        };
        await Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
