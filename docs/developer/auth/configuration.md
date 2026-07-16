# Authentication – Configuration

## Feature Flag

```json
{
  "Features": {
    "Authentication": {
      "Enabled": true,
      "Provider": "Local"
    }
  }
}
```

- `Enabled: false` → all auth endpoints return `501 Not Implemented`,
  `NullAuthProvider` is registered
- `Provider` – selects the active auth provider (`Local` in Phase 1)

## Local Auth Settings

```json
{
  "LocalAuth": {
    "SessionCookieName": "appinventory.auth",
    "SessionIdleTimeoutMinutes": 60,
    "PasswordPolicy": {
      "MinLength": 12,
      "RequireDigit": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": true
    },
    "MaxFailedAttempts": 5,
    "LockoutMinutes": 15
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `SessionCookieName` | string | `appinventory.auth` | Name of the session cookie |
| `SessionIdleTimeoutMinutes` | int | `60` | Cookie max age in minutes |
| `PasswordPolicy:MinLength` | int | `12` | Minimum password length |
| `PasswordPolicy:RequireDigit` | bool | `true` | Require at least one digit |
| `PasswordPolicy:RequireUppercase` | bool | `true` | Require at least one uppercase letter |
| `PasswordPolicy:RequireNonAlphanumeric` | bool | `true` | Require at least one special character |
| `MaxFailedAttempts` | int | `5` | Failed logins before lockout |
| `LockoutMinutes` | int | `15` | Lockout duration in minutes |

## Bootstrap Admin

On first startup, if no `admin` user exists and `ADMIN_INITIAL_PASSWORD`
is set (via environment variable or configuration), the system creates an
initial admin user with `MustChangePassword=true`.

Set the environment variable before first run:

```bash
export ADMIN_INITIAL_PASSWORD="YourSecureInitialPassword123!"
```
