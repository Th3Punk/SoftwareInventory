using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Mcp;

namespace AppInventory.Api.Extensions;

public static class McpServiceExtensions
{
    public static IServiceCollection AddMcpServerFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Mcp");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<IMcpToolset, NullMcpToolset>();
            return services;
        }

        // MCP toolsets will be registered in issue #24
        services.AddSingleton<IMcpToolset, NullMcpToolset>();

        return services;
    }
}
