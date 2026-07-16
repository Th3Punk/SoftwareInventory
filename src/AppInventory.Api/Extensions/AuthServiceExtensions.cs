using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Auth;
using AppInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Api.Extensions;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Authentication");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<IAuthProvider, NullAuthProvider>();
            services.AddSingleton<IGroupProvider, NullGroupProvider>();
            return services;
        }

        var provider = section.GetValue<string>("Provider")
            ?? throw new InvalidOperationException(
                "Features:Authentication:Provider is required when authentication is enabled.");

        switch (provider)
        {
            case "Local":
                services.AddScoped<IAuthProvider, LocalAuthProvider>();
                services.AddSingleton<IGroupProvider, NullGroupProvider>();
                services.AddScoped<PasswordService>();
                services.AddHostedService<AdminBootstrapService>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown authentication provider: '{provider}'");
        }

        return services;
    }

    public static IServiceCollection AddAppDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppInventoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));
        return services;
    }
}
