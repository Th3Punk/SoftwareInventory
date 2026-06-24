using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Docs;

namespace AppInventory.Api.Extensions;

public static class DocumentStoreServiceExtensions
{
    public static IServiceCollection AddDocumentStoreProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Documentation");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<IDocumentStore, NullDocumentStore>();
            return services;
        }

        services.AddSingleton<IDocumentStore, NullDocumentStore>();
        return services;
    }
}
