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

        return provider switch
        {
            "PostgresFts" => services.AddSingleton<ISearchProvider, NullSearchProvider>(),
            _ => throw new InvalidOperationException($"Unknown search provider: '{provider}'")
        };
    }
}
