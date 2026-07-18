# RBAC – Providers

## Authorization Provider Architecture

RBAC is not a pluggable provider itself — it uses the authentication
providers and group providers to resolve user identity and group
memberships, then applies the permission matrix.

### Components

| Component | Role in RBAC |
|---|---|
| `IAuthProvider` | Authenticates the user (provides userId) |
| `IGroupProvider` | Resolves external group memberships |
| `CookieSessionAuthHandler` | Builds claims including roles from UserRoles + GroupRoleMappings |
| `RbacAuthorizeAttribute` | Evaluates IsActive and role requirements |
| `ResourceOwnerAuthorizationHandler` | Evaluates resource ownership |

## Group Role Resolution

When `CookieSessionAuthHandler` authenticates a request:

1. Loads `UserRoles` (directly assigned roles)
2. Loads `ExternalIdentities` with `ExternalGroupsJson`
3. Queries `GroupRoleMappings` for matching active mappings
4. Combines all resolved roles into `ClaimTypes.Role` claims

### Phase 1 (Local Auth)

- No external groups (NullGroupProvider)
- Roles assigned manually via `UserRoles` table
- GroupRoleMapping infrastructure is ready but unused

### Future Phases

- Phase 4: AD/Kerberos groups synced to `ExternalGroupsJson`
- GroupRoleMappings automatically resolve AD groups to roles on login

## Extending RBAC

To add a new permission:

1. Add a constant to `PolicyNames.cs`
2. Register the policy in `AuthorizationServiceExtensions.AddRbacAuthorization()`
3. Apply `[RbacAuthorize]` or `[Authorize(Policy = "...")]` to endpoints
4. Update the permission matrix documentation
