# Application Catalog - Overview

## Purpose

The application catalog is the core module of SoftwareInventory. It manages the registry of internal software applications along with their metadata, environment URLs, contacts, and tags.

## Architecture

### Entities (Core Layer)

| Entity | Description |
|--------|-------------|
| `Application` | Central entity with name, description, status, type, source control, and soft-delete support |
| `ApplicationEnvironment` | Environment URLs (Development/Test/UAT/Production) with `IsPublic` visibility flag |
| `ApplicationContact` | Contact info (Email/Slack/Teams/SupportUrl/Phone) with optional label |
| `Tag` | Reusable label with optional hex color code |
| `ApplicationTag` | Many-to-many join table between Application and Tag |

### Controllers (API Layer)

- `ApplicationsController` - Full CRUD for applications plus nested environment and contact endpoints
- `TagsController` - Tag list/create/delete (Admin only for mutations)

### Authorization

The catalog uses the RBAC middleware from issue #10:

| Operation | Allowed Roles |
|-----------|---------------|
| List/Get applications | All authenticated (ReadOnly+) |
| Create application | Developer, Admin |
| Edit application | ApplicationOwner (own only), Developer, Admin |
| Delete application (soft) | Admin |
| Environment/Contact CRUD | Developer, Admin |
| Tag create/delete | Admin |
| View non-public environments | ApplicationOwner, Developer, Admin |

### Key Design Decisions

- **Soft delete**: Applications are never physically deleted; `IsDeleted = true` and EF Core global query filter excludes them from normal queries
- **Source control field**: Dedicated `SourceControl` enum (None/Git/AzureDevOps) plus `RepositoryUrl` instead of a generic links table, per spec 9.4
- **URL validation**: Only `http`/`https` schemes accepted for `RepositoryUrl`, `WikiUrl`, and environment URLs
- **Tag normalization**: Tag names are stored lowercase for case-insensitive uniqueness
- **IsPublic environments**: Non-public environments are filtered at query level using `QueryAuthorizationFilter.CanViewNonPublicEnvironments()`

### Evaluation Flow

```text
Request → Auth (401) → FeatureGate "ApplicationCatalog" (501) → IsActive (403) → Role (403) → Resource Owner (403) → Action
```
