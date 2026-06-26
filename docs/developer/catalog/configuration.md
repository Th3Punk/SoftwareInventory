# Application Catalog - Configuration

## Feature Flag

```json
{
  "Features": {
    "ApplicationCatalog": {
      "Enabled": true
    }
  }
}
```

When disabled, all `/api/v1/applications` and `/api/v1/tags` endpoints return `501 Not Implemented`.

## Pagination Defaults

| Parameter | Default | Max |
|-----------|---------|-----|
| `page` | 1 | - |
| `pageSize` | 20 | 100 |

## Sorting Options

| Value | Description |
|-------|-------------|
| `name` | Sort by name ascending |
| `-name` | Sort by name descending |
| `createdAt` | Sort by creation date ascending |
| `-createdAt` | Sort by creation date descending |
| `updatedAt` | Sort by last update ascending |
| `-updatedAt` | Sort by last update descending |

Default sort: `name` (ascending).

## Filter Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | enum | `Active`, `Maintenance`, `Deprecated`, `Retired` |
| `type` | enum | `WebApp`, `ApiService`, `Library`, `BatchJob`, `MobileApp`, `Other` |
| `team` | string | Partial match on `OwnerTeam` |
| `tag` | string[] | Tag name(s); AND logic when multiple |
| `q` | string | Simple text search on name, description, and team |

## Database Tables

| Table | Key | Notable Constraints |
|-------|-----|---------------------|
| `Applications` | `Id` (identity) | `Name` unique index, `IsDeleted` global query filter |
| `ApplicationEnvironments` | `Id` (identity) | FK to Applications (cascade) |
| `ApplicationContacts` | `Id` (identity) | FK to Applications (cascade) |
| `Tags` | `Id` (identity) | `Name` unique index |
| `ApplicationTags` | `(ApplicationId, TagId)` | Composite PK, cascade delete both sides |

## Migration

Migration name: `20250624120000_Catalog_ApplicationSchema`

SQL script: `sql/migrations/20250624120000_Catalog_ApplicationSchema.sql`
