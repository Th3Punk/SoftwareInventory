# Application Catalog - Providers

## Architecture

The application catalog does not follow the pluggable provider pattern used by Authentication, Search, or Audit modules. It is a direct CRUD domain backed by EF Core and PostgreSQL.

However, it integrates with the following pluggable providers:

### Search Provider (ISearchProvider)

When the `q` query parameter is used on `GET /api/v1/applications`, the catalog currently performs a simple `LIKE`-based search on `Name`, `ShortDescription`, and `OwnerTeam`. In a future phase, this will delegate to `ISearchProvider` (PostgreSQL full-text search or Elasticsearch) for ranked results.

### Audit Provider (IAuditProvider)

In a future phase, catalog mutations (create, update, delete) will be logged via `IAuditProvider`. The audit entries will include the resource type (`Application`, `ApplicationEnvironment`, `ApplicationContact`, `Tag`), the action performed, and the old/new values as JSON.

## Authorization Integration

The catalog uses the RBAC system from the `Authorization` domain:

- **FeatureGateAttribute**: Controls `501` response when `ApplicationCatalog` feature is disabled
- **RbacAuthorizeAttribute**: Enforces role-based access per endpoint
- **ResourceOwnerRequirement**: Used for `PUT /api/v1/applications/{id}` to allow `ApplicationOwner` to edit only their own applications
- **QueryAuthorizationFilter**: Used to filter non-public environments at query level

## Future Extensions

- Full-text search integration via `ISearchProvider`
- Audit logging via `IAuditProvider`
- Application import/export
- Application dependency graph
