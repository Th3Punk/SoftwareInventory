namespace AppInventory.Core.Interfaces;

public interface ISearchProvider
{
    bool IsAvailable { get; }
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
}

public record SearchQuery(string Term, string? ResourceType = null, int Page = 1, int PageSize = 20);

public record SearchResultItem(string ResourceType, string ResourceId, string Title, string? Excerpt = null, double Score = 0);

public record SearchResult(IReadOnlyList<SearchResultItem> Items, int TotalCount)
{
    public static SearchResult Unavailable() => new([], 0);
}
