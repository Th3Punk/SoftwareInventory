using AppInventory.Api.Middleware;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Mcp;
using Microsoft.AspNetCore.Authentication;

namespace AppInventory.Api.Extensions;

public static class McpServiceExtensions
{
    public static IServiceCollection AddMcpToolset(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Mcp");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<IMcpToolset, NullMcpToolset>();
            return services;
        }

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, McpBearerAuthHandler>(
                McpBearerAuthDefaults.AuthenticationScheme, null);

        services.AddAuthorizationBuilder()
            .AddPolicy("McpAccess", p =>
                p.RequireAuthenticatedUser()
                 .AddAuthenticationSchemes(McpBearerAuthDefaults.AuthenticationScheme));

        services.AddMcpServer()
            .WithHttpTransport(o => o.Stateless = section.GetValue("Stateless", true))
            .WithTools<CatalogMcpToolset>()
            .WithTools<DocumentationMcpToolset>();

        services.AddSingleton<IMcpToolset, LiveMcpToolset>();
        return services;
    }
}
