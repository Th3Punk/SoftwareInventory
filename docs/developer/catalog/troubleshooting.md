# Application Catalog - Troubleshooting

## Common Issues

### 501 "Feature Not Available" on catalog endpoints

**Cause:** The `ApplicationCatalog` feature flag is disabled.

**Fix:** Enable in `appsettings.json`:

```json
{
  "Features": {
    "ApplicationCatalog": {
      "Enabled": true
    }
  }
}
```

### 400 "RepositoryUrl is required when SourceControl is not None"

**Cause:** The application was created or updated with `SourceControl` set to `Git` or `AzureDevOps` but no `RepositoryUrl` was provided.

**Fix:** Either provide a valid `http`/`https` URL in `RepositoryUrl`, or set `SourceControl` to `None`.

### 400 "URL has an invalid scheme"

**Cause:** A `RepositoryUrl`, `WikiUrl`, or environment `Url` was provided with a non-HTTP scheme (e.g., `ftp://`, `ssh://`).

**Fix:** Only `http://` and `https://` URLs are accepted. Convert the URL to use an HTTP scheme.

### 409 "Application with name already exists"

**Cause:** An application with the same `Name` already exists (including soft-deleted ones).

**Fix:** Use a different name, or check for soft-deleted applications:

```sql
SELECT "Id", "Name", "IsDeleted" FROM "Applications"
WHERE "Name" = '<name>';
```

### 403 "You can only edit applications you own"

**Cause:** A user with `ApplicationOwner` role tried to edit an application they did not create.

**Fix:** The `ApplicationOwner` role can only edit applications where `CreatedByUserId` matches the authenticated user's ID. Users with `Developer` or `Admin` roles can edit any application.

### Non-public environments not visible

**Cause:** The authenticated user has `ReadOnly` role, which cannot see environments where `IsPublic = false`.

**Fix:** Only `ApplicationOwner`, `Developer`, and `Admin` roles can view non-public environments. Assign the appropriate role to the user.

### Soft-deleted applications not appearing

**Cause:** EF Core global query filter automatically excludes applications where `IsDeleted = true`.

**Fix:** This is by design. To query deleted applications directly:

```sql
SELECT * FROM "Applications" WHERE "IsDeleted" = true;
```

### Tag names are case-insensitive

**Cause:** Tags are normalized to lowercase on creation. Creating "JavaScript" stores "javascript".

**Fix:** This is by design for consistent filtering. Always compare tag names in lowercase.
