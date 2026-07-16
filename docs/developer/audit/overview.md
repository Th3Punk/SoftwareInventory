# Audit Log – Developer Overview

## Purpose

The audit log records all write operations (create, update, delete, status changes) performed through the API. Every entry captures who did what, to which resource, and from where.

## Architecture

```
IAuditProvider (Core/Interfaces/)
├── NullAuditProvider       – no-op, used when audit is disabled
└── DatabaseAuditProvider   – writes to AuditLogs table via EF Core
```

`DatabaseAuditProvider` is registered as a **Singleton** and uses `IServiceScopeFactory` to create a new DI scope per log entry, avoiding the lifetime mismatch with the Scoped `AppInventoryDbContext`.

## Key Classes

| Class | Location | Role |
|---|---|---|
| `IAuditProvider` | `Core/Interfaces/` | Contract |
| `AuditLog` | `Core/Entities/AuditLog.cs` | Entity |
| `NullAuditProvider` | `Infrastructure/Audit/` | Disabled state |
| `DatabaseAuditProvider` | `Infrastructure/Audit/` | EF Core writer |
| `AuditServiceExtensions` | `Api/Extensions/` | DI registration |

## Integration Points

Controllers call `_audit.LogAsync(...)` after every successful state-changing database operation. The `IAuditProvider` is injected via the constructor.

Audited actions:

- `ApplicationsController`: Created, Updated, Deleted
- `DocumentationsController`: Created, Updated, StatusChanged, Archived
