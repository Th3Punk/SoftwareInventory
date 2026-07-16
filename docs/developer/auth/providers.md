# Authentication – Providers

## Available Providers

### LocalAuthProvider (Phase 1 – Active)

Database-based username/password authentication with cookie sessions.

- Password hashing: ASP.NET Core `PasswordHasher<T>` (PBKDF2, per-user salt)
- Session: `HttpOnly` + `Secure` + `SameSite=Lax` cookie
- Account lockout after configurable failed attempts
- Password policy enforcement on change

**Configuration:** `Features:Authentication:Provider = "Local"`

### NullAuthProvider

Used when `Features:Authentication:Enabled = false`. All authentication
calls return failure. No endpoints are functional (501 from FeatureGate).

## Group Providers

### NullGroupProvider (Phase 1 – Active)

No external group source. Roles are assigned manually via the `UserRole`
table with `Source = Manual`.

### Future Providers (Phase 4)

- `KerberosAuthProvider` + `LdapGroupProvider` – AD/Kerberos SSO
- `OAuthProvider` – OIDC authorization code flow
- `OktaProvider` + `OktaGroupProvider` – Okta integration

## Switching Providers

To switch auth providers, update `appsettings.json`:

```json
{
  "Features": {
    "Authentication": {
      "Enabled": true,
      "Provider": "Kerberos"
    }
  }
}
```

Restart the application. The DI extension (`AddAuthProvider`) will
register the matching provider implementation.
