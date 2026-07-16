# Audit Log – Operations

## Environment Variables / Config

| Config Key | Description | Example |
|---|---|---|
| `Features__AuditLog__Enabled` | Enable/disable audit logging | `true` |
| `Features__AuditLog__Provider` | Provider selection | `Database` |

These map to `appsettings.json` keys via ASP.NET Core's `__` separator convention and can be set as Kubernetes ConfigMap or Secret entries.

## Database

The `AuditLogs` table is created by migration `202507161400_Audit_AuditLog`.

Indexes:

- `IX_AuditLogs_Timestamp` – range queries by time
- `IX_AuditLogs_UserId` – per-user activity lookup
- `IX_AuditLogs_ResourceType_ResourceId` – per-resource history

## Rollback

To disable audit logging without a code deploy, set `Features__AuditLog__Enabled=false` and restart the pod. The `NullAuditProvider` will be used; the table is preserved.

To roll back the migration (removes the table and all data):

```sql
DROP TABLE "AuditLogs";
```

## Retention

No automated retention policy is implemented. Monitor table size and truncate or archive old rows as needed.
