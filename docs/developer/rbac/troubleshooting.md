# RBAC – Troubleshooting

## Common Issues

### 403 "User account is deactivated"

**Cause:** The user's `IsActive` flag is `false` in the database.

**Fix:** Reactivate the user:

```sql
UPDATE "Users" SET "IsActive" = true WHERE "Id" = <user_id>;
```

### 403 "You do not have the required role"

**Cause:** The user lacks the required role for the endpoint.

**Fix:** Assign the appropriate role:

```sql
INSERT INTO "UserRoles" ("UserId", "RoleId", "GrantedAt", "Source")
VALUES (<user_id>, <role_id>, NOW(), 'Manual');
```

Role IDs: 1=Admin, 2=Developer, 3=ApplicationOwner, 4=ReadOnly.

### 501 "Feature Not Available" on RBAC-dependent endpoints

**Cause:** The corresponding feature flag is disabled in `appsettings.json`.

**Fix:** Enable the feature:

```json
{
  "Features": {
    "Authorization": {
      "Enabled": true
    }
  }
}
```

### Roles from GroupRoleMapping not applied

**Cause:** Either the `GroupRoleMapping.IsActive` is `false`, or
`ExternalIdentity.ExternalGroupsJson` does not contain the expected
group reference.

**Fix:**

1. Verify the mapping exists and is active:

    ```sql
    SELECT * FROM "GroupRoleMappings"
    WHERE "IsActive" = true
    AND "ExternalGroupRef" = '<group_ref>';
    ```

2. Check the user's external identity groups:

    ```sql
    SELECT "ExternalGroupsJson" FROM "ExternalIdentities"
    WHERE "UserId" = <user_id>;
    ```

3. Ensure `ExternalGroupsJson` is valid JSON array format:
   `["CN=Group1,DC=example,DC=com", "CN=Group2,DC=example,DC=com"]`

### Resource ownership check fails for ApplicationOwner

**Cause:** The `ResourceOwnerAuthorizationHandler` compares the
authenticated user's ID with the resource owner's user ID. If these
don't match, authorization fails.

**Fix:** Verify the application's owner user ID matches the
authenticated user. Admins bypass this check.
