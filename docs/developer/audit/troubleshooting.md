# Audit Log – Troubleshooting

## Audit entries are not being written

1. Check `Features:AuditLog:Enabled` is `true` in the active config.
2. Check `Features:AuditLog:Provider` is set to `"Database"`.
3. Verify the `AuditLogs` table exists (migration `202507161400_Audit_AuditLog` must have run).

## `InvalidOperationException: Unknown audit provider`

The `Provider` value in config does not match any registered case. Currently only `"Database"` is valid.

## High write latency on audited endpoints

Each `LogAsync` call opens a new DB scope and writes synchronously within the request pipeline. For high-throughput scenarios, consider replacing `DatabaseAuditProvider` with an async queue-backed provider.

## AuditLogs table growing large

Add a periodic cleanup job or configure PostgreSQL table partitioning on the `Timestamp` column. No built-in retention policy exists in v1.
