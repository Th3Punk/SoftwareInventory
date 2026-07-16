# Audit Log – Providers

## NullAuditProvider

- `IsAvailable`: `false`
- All calls are no-ops; returns immediately without throwing.
- Used when `Features:AuditLog:Enabled` is `false`.

## DatabaseAuditProvider

- `IsAvailable`: `true`
- Writes one row to the `AuditLogs` table per `LogAsync` call.
- Uses `IServiceScopeFactory` to avoid DI lifetime issues (Singleton service, Scoped DbContext).
- Thread-safe: each call creates its own scope and disposes it after saving.

### Switching Providers

To replace `DatabaseAuditProvider` with a custom provider (e.g., a message queue writer):

1. Create `MyProvider : IAuditProvider` in `Infrastructure/Audit/`
2. Add `"MyProvider" => services.AddSingleton<IAuditProvider, MyProvider>()` to the switch in `AuditServiceExtensions`
3. Set `Features:AuditLog:Provider: "MyProvider"` in configuration
4. No changes required in controllers or Core layer
