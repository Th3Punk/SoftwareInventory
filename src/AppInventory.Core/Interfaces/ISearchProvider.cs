namespace AppInventory.Core.Interfaces;

public interface ISearchProvider
{
    bool IsAvailable { get; }
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
}

public record SearchQuery(
    string Term,
    IReadOnlyList<string>? ResourceTypes = null,
    IReadOnlyList<string>? Tags = null,
    int Page = 1,
    int PageSize = 20);

public record SearchResultItem(
    string ResourceType,
    int ResourceId,
    string Title,
    string Snippet,
    double Score);

public record SearchResult(
    IReadOnlyList<SearchResultItem> Items,
    int TotalCount,
    bool IsAvailable)
{
    public static SearchResult Unavailable()
        => new([], 0, IsAvailable: false);
}
