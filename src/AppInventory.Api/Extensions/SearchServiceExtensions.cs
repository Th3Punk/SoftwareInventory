using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Search;

namespace AppInventory.Api.Extensions;

public static class SearchServiceExtensions
{
    public static IServiceCollection AddSearchProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Search");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<ISearchProvider, NullSearchProvider>();
            return services;
        }

        var provider = section.GetValue<string>("Provider")
            ?? throw new InvalidOperationException(
                "Features:Search:Provider is required when search is enabled.");

        switch (provider)
        {
            case "PostgresFts":
                // PostgresFtsSearchProvider will be registered in issue #15
                services.AddSingleton<ISearchProvider, NullSearchProvider>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown search provider: '{provider}'");
        }

        return services;
    }
}
