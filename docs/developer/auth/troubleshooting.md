# Authentication – Troubleshooting

## Common Issues

### "Authentication is not configured" on login

**Cause:** `Features:Authentication:Enabled` is `false` or missing.

**Fix:** Set `"Enabled": true` and `"Provider": "Local"` in
`appsettings.json` under `Features:Authentication`.

### Bootstrap admin not created

**Cause:** `ADMIN_INITIAL_PASSWORD` environment variable is not set.

**Fix:** Set the variable before first startup:

```bash
export ADMIN_INITIAL_PASSWORD="SecureP@ssw0rd123!"
```

The admin is only created once. If you need to reset, delete the user from
the database and restart.

### Account locked out

**Cause:** Too many failed login attempts (`MaxFailedAttempts` exceeded).

**Fix:** Wait for `LockoutMinutes` to expire, or clear the lockout
in the database:

```sql
UPDATE "LocalCredentials"
SET "FailedAttempts" = 0, "LockedUntil" = NULL
WHERE "UserId" = <user_id>;
```

### "Password Change Required" on every request

**Cause:** User has `MustChangePassword = true` (bootstrap admin or
admin-reset password).

**Fix:** Call `POST /api/v1/auth/change-password` with current and
new password. The middleware blocks all other endpoints until the
password is changed.

### Cookie not being set

**Cause:** Running over HTTP (non-HTTPS) in production. The `Secure`
flag prevents the cookie from being sent over insecure connections.

**Fix:** Use HTTPS in production. In development, the cookie is still
set but may require `Secure=false` adjustment if not using HTTPS locally.
