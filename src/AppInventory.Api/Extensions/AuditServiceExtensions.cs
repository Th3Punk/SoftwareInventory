using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Audit;

namespace AppInventory.Api.Extensions;

public static class AuditServiceExtensions
{
    public static IServiceCollection AddAuditProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:AuditLog");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<IAuditProvider, NullAuditProvider>();
            return services;
        }

        var provider = section.GetValue<string>("Provider")
            ?? throw new InvalidOperationException(
                "Features:AuditLog:Provider is required when audit is enabled.");

        return provider switch
        {
            "Database" => services.AddSingleton<IAuditProvider, NullAuditProvider>(),
            _ => throw new InvalidOperationException($"Unknown audit provider: '{provider}'")
        };
    }
}
