# RBAC – Configuration

## Feature Flag

```json
{
  "Features": {
    "Authorization": {
      "Enabled": true
    }
  }
}
```

When `Enabled: false`, the `FeatureGateAttribute` on authorization-dependent
endpoints returns 501 Not Implemented.

## Authorization Policies

Policies are registered in `AddRbacAuthorization()` and map directly to the
permission matrix (spec 8.2):

| Policy Name | Allowed Roles |
|---|---|
| `Application.Read` | ReadOnly, ApplicationOwner, Developer, Admin |
| `Application.Create` | Developer, Admin |
| `Application.EditOwn` | ApplicationOwner, Developer, Admin |
| `Application.EditAny` | Admin |
| `Application.Delete` | Admin |
| `UserDoc.Read` | ReadOnly, ApplicationOwner, Developer, Admin |
| `UserDoc.Write` | ApplicationOwner, Developer, Admin |
| `DeveloperDoc.Read` | Developer, Admin |
| `DeveloperDoc.Write` | Developer, Admin |
| `OpsDoc.ReadWrite` | Admin |
| `AuditLog.Read` | Admin |
| `GroupMapping.Manage` | Admin |
| `User.Manage` | Admin |
| `NonPublicEnvironments` | ApplicationOwner, Developer, Admin |

## Role Seeding

System roles are seeded via EF Core migration `20250618120000_Auth_InitialSchema`:

| Id | Name | IsSystemRole |
|---|---|---|
| 1 | Admin | true |
| 2 | Developer | true |
| 3 | ApplicationOwner | true |
| 4 | ReadOnly | true |

## GroupRoleMapping Configuration

Administrators configure group-to-role mappings via the `GroupRoleMappings`
table. Each mapping links an external group reference (e.g., AD group DN)
to a system role:

| Column | Description |
|---|---|
| `ProviderType` | Auth provider (Local, ActiveDirectory, OAuth, Okta) |
| `ExternalGroupRef` | External group identifier (e.g., AD group DN) |
| `RoleId` | Target role ID |
| `IsActive` | Enable/disable individual mappings |
