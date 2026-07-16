using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AppInventory.Core.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AppInventory.Api.Middleware;

public static class McpBearerAuthDefaults
{
    public const string AuthenticationScheme = "McpServiceToken";
}

internal sealed class McpBearerAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public McpBearerAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.Fail("Empty bearer token."));
        }

        var validTokens = _configuration.GetSection("Features:Mcp:ServiceTokens")
            .Get<string[]>() ?? [];

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var isValid = validTokens.Any(t =>
        {
            var configuredBytes = Encoding.UTF8.GetBytes(t);
            if (configuredBytes.Length != tokenBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(configuredBytes, tokenBytes);
        });

        if (!isValid)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid service token."));
        }

        var defaultRole = _configuration.GetValue("Features:Mcp:DefaultRole", RoleNames.ReadOnly)!;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mcp-service"),
            new Claim(ClaimTypes.Name, "MCP Service"),
            new Claim(ClaimTypes.Role, defaultRole),
            new Claim(ClaimNames.IsActive, "true")
        };

        var identity = new ClaimsIdentity(claims, McpBearerAuthDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, McpBearerAuthDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/problem+json";
        await Response.WriteAsync(
            """{"type":"https://tools.ietf.org/html/rfc7807","title":"Unauthorized","status":401,"detail":"A valid MCP service token is required."}""");
    }
}
