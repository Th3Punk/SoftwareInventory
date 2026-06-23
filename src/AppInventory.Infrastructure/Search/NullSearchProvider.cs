using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Search;

internal sealed class NullSearchProvider : ISearchProvider
{
    public bool IsAvailable => false;

    public Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct)
        => Task.FromResult(SearchResult.Unavailable());
}
