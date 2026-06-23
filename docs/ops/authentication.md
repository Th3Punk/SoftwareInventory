# Authentication – Operations Guide

## Environment Variables

| Variable | Required | Description |
|---|---|---|
| `ADMIN_INITIAL_PASSWORD` | First run only | Initial password for the bootstrap admin user. Set via K8s Secret or Docker env. |

## Kubernetes Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: appinventory-auth
type: Opaque
stringData:
  ADMIN_INITIAL_PASSWORD: "YourSecureInitialPassword123!"
```

Mount as environment variable in the deployment:

```yaml
envFrom:
  - secretRef:
      name: appinventory-auth
```

## Configuration Keys

All keys under `Features:Authentication` and `LocalAuth` in
`appsettings.json`. See `docs/developer/auth/configuration.md`
for the full list.

## Session Management

Sessions are cookie-based with configurable idle timeout
(`SessionIdleTimeoutMinutes`, default 60). No server-side session
store is required — the cookie contains the user ID and is
validated against the database on each request.

## Account Lockout

Default: 5 failed attempts, 15-minute lockout. Configurable via
`LocalAuth:MaxFailedAttempts` and `LocalAuth:LockoutMinutes`.

To manually unlock an account:

```sql
UPDATE "LocalCredentials"
SET "FailedAttempts" = 0, "LockedUntil" = NULL
WHERE "UserId" = <user_id>;
```

## Rollback

To disable authentication entirely:

```json
{
  "Features": {
    "Authentication": {
      "Enabled": false
    }
  }
}
```

This registers `NullAuthProvider` and all auth endpoints return 501.

To revert the database migration:

```bash
dotnet ef database update 0 --project src/AppInventory.Infrastructure
```

Or manually run the rollback SQL:

```sql
DROP TABLE IF EXISTS "GroupRoleMappings";
DROP TABLE IF EXISTS "UserRoles";
DROP TABLE IF EXISTS "RolePermissions";
DROP TABLE IF EXISTS "LocalCredentials";
DROP TABLE IF EXISTS "ExternalIdentities";
DROP TABLE IF EXISTS "Permissions";
DROP TABLE IF EXISTS "Roles";
DROP TABLE IF EXISTS "Users";
```
