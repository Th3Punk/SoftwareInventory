# Search – Providers

## NullSearchProvider

- `IsAvailable`: `false`
- Returns `SearchResult.Unavailable()` immediately.
- Used when `Features:Search:Enabled` is `false`.

## PostgresFtsSearchProvider

- `IsAvailable`: `true`
- Uses PostgreSQL `tsvector` + `websearch_to_tsquery` + `ts_rank_cd`.
- Queries Applications and Documentations separately, merges and sorts by score.
- Applies `AllowedDocumentationTypes` filter (from caller's role) at SQL level.
- Requires the GIN indexes from migration `202507161500_Search_FtsIndexes` for performance.

### Adding a New Provider (e.g. Elasticsearch)

1. Implement `ISearchProvider` in `Infrastructure/Search/`
2. Add `"Elasticsearch" => services.AddSingleton<ISearchProvider, ElasticsearchSearchProvider>()` to `SearchServiceExtensions`
3. Set `Features:Search:Provider: "Elasticsearch"` in config
4. No changes to `SearchController` or Core layer required
