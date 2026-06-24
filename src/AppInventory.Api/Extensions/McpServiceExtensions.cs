using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Mcp;

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

        services.AddSingleton<IMcpToolset, NullMcpToolset>();
        return services;
    }
}
