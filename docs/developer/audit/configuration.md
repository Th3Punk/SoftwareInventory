# Audit Log – Configuration

## Config Keys

```json
{
  "Features": {
    "AuditLog": {
      "Enabled": true,
      "Provider": "Database"
    }
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Features:AuditLog:Enabled` | bool | `false` | Enables audit logging. When false, `NullAuditProvider` is used. |
| `Features:AuditLog:Provider` | string | required if Enabled | Provider name. Currently only `"Database"` is supported. |

## Disabling Audit

Set `Enabled: false`. No table writes occur; `NullAuditProvider.IsAvailable` returns `false`.

## Adding a New Provider

1. Implement `IAuditProvider` in `AppInventory.Infrastructure/Audit/`
2. Add a case to the switch in `AuditServiceExtensions.AddAuditProvider`
3. Register the new provider string in this configuration doc
