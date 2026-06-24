using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Notifications;

namespace AppInventory.Api.Extensions;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Notifications");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<INotificationProvider, NullNotificationProvider>();
            return services;
        }

        var provider = section.GetValue<string>("Provider")
            ?? throw new InvalidOperationException(
                "Features:Notifications:Provider is required when notifications are enabled.");

        return provider switch
        {
            "Null" => services.AddSingleton<INotificationProvider, NullNotificationProvider>(),
            _ => throw new InvalidOperationException($"Unknown notification provider: '{provider}'")
        };
    }
}
