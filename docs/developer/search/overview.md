# Search – Developer Overview

## Purpose

Full-text search across the application catalog and documentation using PostgreSQL's built-in FTS engine. Results are RBAC-filtered: documentation type visibility follows the same rules as the Documentation API.

## Architecture

```
ISearchProvider (Core/Interfaces/)
├── NullSearchProvider       – IsAvailable=false, returns empty result
└── PostgresFtsSearchProvider – raw SQL with tsvector/GIN indexes
```

The provider is Singleton and uses `IServiceScopeFactory` to obtain a scoped `AppInventoryDbContext`.

## Key Classes

| Class | Location | Role |
|---|---|---|
| `ISearchProvider` | `Core/Interfaces/` | Contract |
| `SearchQuery` | `Core/Interfaces/` | Input (includes `AllowedDocumentationTypes` for RBAC) |
| `SearchResult` | `Core/Interfaces/` | Output |
| `NullSearchProvider` | `Infrastructure/Search/` | Disabled state |
| `PostgresFtsSearchProvider` | `Infrastructure/Search/` | Production provider |
| `SearchController` | `Api/Controllers/` | `GET /api/v1/search` |

## FTS Schema

Migration `202507161500_Search_FtsIndexes` adds a stored generated `tsvector` column and GIN index to both tables:

- `Applications."SearchVector"` = `to_tsvector('simple', Name || ShortDescription || DetailedDescription || OwnerTeam)`
- `Documentations."SearchVector"` = `to_tsvector('simple', Title || Content)`

Queries use `websearch_to_tsquery` (supports `AND`, `OR`, `NOT`, phrase search) and rank with `ts_rank_cd`.

## RBAC Filtering

The controller resolves `AllowedDocumentationTypes` from the caller's role:

| Role | Allowed doc types |
|---|---|
| Admin | `null` (all) |
| Developer | `["User", "Developer"]` |
| ReadOnly / ApplicationOwner | `["User"]` |
