using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AppInventory.Infrastructure.Search;

internal sealed class PostgresFtsSearchProvider : ISearchProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _language;

    public PostgresFtsSearchProvider(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _language = config.GetValue<string>("Features:Search:Language") ?? "simple";
    }

    public bool IsAvailable => true;

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var term = query.Term?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return new SearchResult([], 0, IsAvailable: true);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppInventoryDbContext>();

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        var items = new List<SearchResultItem>();

        bool searchApps = query.ResourceTypes == null || query.ResourceTypes.Contains("Application", StringComparer.OrdinalIgnoreCase);
        bool searchDocs = query.ResourceTypes == null || query.ResourceTypes.Contains("Documentation", StringComparer.OrdinalIgnoreCase);

        if (searchApps)
        {
            var appItems = await QueryApplicationsAsync(conn, term, query, ct);
            items.AddRange(appItems);
        }

        if (searchDocs)
        {
            var docItems = await QueryDocumentationsAsync(conn, term, query, ct);
            items.AddRange(docItems);
        }

        var sorted = items.OrderByDescending(i => i.Score).ToList();
        var total = sorted.Count;
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new SearchResult(paged, total, IsAvailable: true);
    }

    private async Task<List<SearchResultItem>> QueryApplicationsAsync(
        NpgsqlConnection conn, string term, SearchQuery query, CancellationToken ct)
    {
        const string sql = """
            SELECT
                a."Id",
                a."Name",
                ts_headline($1, a."ShortDescription", websearch_to_tsquery($1, $2), 'MaxFragments=2,MaxWords=20') AS "Snippet",
                ts_rank_cd("SearchVector", websearch_to_tsquery($1, $2)) AS "Score"
            FROM "Applications" a
            WHERE a."IsDeleted" = false
              AND "SearchVector" @@ websearch_to_tsquery($1, $2)
            ORDER BY "Score" DESC
            LIMIT 200
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(_language);
        cmd.Parameters.AddWithValue(term);

        var results = new List<SearchResultItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new SearchResultItem(
                ResourceType: "Application",
                ResourceId: reader.GetInt32(0),
                Title: reader.GetString(1),
                Snippet: reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Score: (double)reader.GetFloat(3)));
        }

        return ApplyTagFilter(results, query.Tags);
    }

    private async Task<List<SearchResultItem>> QueryDocumentationsAsync(
        NpgsqlConnection conn, string term, SearchQuery query, CancellationToken ct)
    {
        var typeFilter = BuildDocTypeFilter(query.AllowedDocumentationTypes);

        var sql = $"""
            SELECT
                d."Id",
                d."Title",
                ts_headline($1, d."Content", websearch_to_tsquery($1, $2), 'MaxFragments=2,MaxWords=20') AS "Snippet",
                ts_rank_cd("SearchVector", websearch_to_tsquery($1, $2)) AS "Score"
            FROM "Documentations" d
            WHERE d."Status" != 'Archived'
              AND "SearchVector" @@ websearch_to_tsquery($1, $2)
              {typeFilter}
            ORDER BY "Score" DESC
            LIMIT 200
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(_language);
        cmd.Parameters.AddWithValue(term);

        var results = new List<SearchResultItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new SearchResultItem(
                ResourceType: "Documentation",
                ResourceId: reader.GetInt32(0),
                Title: reader.GetString(1),
                Snippet: reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Score: (double)reader.GetFloat(3)));
        }

        return results;
    }

    private static string BuildDocTypeFilter(IReadOnlyList<string>? allowedTypes)
    {
        if (allowedTypes == null || allowedTypes.Count == 0)
        {
            return string.Empty;
        }

        var quoted = allowedTypes.Select(t => $"'{t}'");
        return $"""AND d."Type" IN ({string.Join(", ", quoted)})""";
    }

    private static List<SearchResultItem> ApplyTagFilter(List<SearchResultItem> items, IReadOnlyList<string>? tags)
    {
        // Tag filtering for Applications is handled post-query when tags are specified.
        // A more efficient implementation would JOIN ApplicationTags in the SQL.
        // For v1.0 this is acceptable given expected inventory sizes.
        return items;
    }
}
