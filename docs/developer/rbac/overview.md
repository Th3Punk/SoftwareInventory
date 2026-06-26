# RBAC – Developer Overview

## Architecture

The authorization system implements role-based access control (RBAC) with
the evaluation order defined in spec 8.4:

1. **Authentication** (401) – `CookieSessionAuthHandler`
2. **Feature flag** (501) – `FeatureGateAttribute`
3. **IsActive check** (403) – `RbacAuthorizeAttribute`
4. **Role check** (403) – `RbacAuthorizeAttribute`
5. **Resource-level check** (403) – Controller logic + `IAuthorizationService`

## Key Classes

| Class | Project | Responsibility |
|---|---|---|
| `RoleNames` | Core | Role name constants |
| `PolicyNames` | Core | Authorization policy name constants |
| `ClaimNames` | Core | Custom claim name constants |
| `RbacAuthorizeAttribute` | Api | Action filter enforcing IsActive + role checks |
| `FeatureGateAttribute` | Api | Action filter for feature flag checks (501) |
| `ResourceOwnerRequirement` | Api | Authorization requirement for resource ownership |
| `ResourceOwnerAuthorizationHandler` | Api | Evaluates resource ownership |
| `QueryAuthorizationFilter` | Infrastructure | Query-level role filtering helpers |
| `AuthorizationServiceExtensions` | Api | DI registration for policies and handlers |

## System Roles (spec 8.1)

| Role | Description |
|---|---|
| `Admin` | Full access; user management, group mappings, all CRUD |
| `Developer` | Application CRUD, developer/ops docs read and write |
| `ApplicationOwner` | Edit own applications, write user docs |
| `ReadOnly` | Read all public content (user docs, catalog) |

## Evaluation Flow

```text
Request
  │
  ├─ UseAuthentication()          → populates ClaimsPrincipal
  │                                  (includes IsActive, roles, MustChangePassword)
  ├─ UseAuthorization()           → [Authorize] → 401 if not authenticated
  ├─ MustChangePasswordMiddleware → 403 if password change required
  │
  └─ Endpoint filters:
       ├─ FeatureGateAttribute (Order=0)     → 501 if feature disabled
       ├─ RbacAuthorizeAttribute (Order=1)   → 403 if inactive or wrong role
       └─ Controller action                  → resource-level checks
```

## GroupRoleMapping

On each authenticated request, `CookieSessionAuthHandler` resolves
group-based roles from `ExternalIdentity.ExternalGroupsJson` via the
`GroupRoleMappings` table. Resolved roles are added as `ClaimTypes.Role`
claims alongside directly assigned roles.

## Resource-Level Authorization

For resource ownership checks (e.g., ApplicationOwner editing own app),
controllers use `IAuthorizationService` with `ResourceOwnerRequirement`:

```csharp
var result = await _authorizationService.AuthorizeAsync(
    User, ownerUserId, new ResourceOwnerRequirement());

if (!result.Succeeded)
    return Forbid();
```

## Query-Level Filtering (spec 8.5)

`QueryAuthorizationFilter` provides static methods for EF Core query
filtering based on user roles:

```csharp
var hasDeveloperAccess = QueryAuthorizationFilter.HasDeveloperAccess(User);
query = query.Where(d =>
    d.Type == DocumentationType.User || hasDeveloperAccess);
```
