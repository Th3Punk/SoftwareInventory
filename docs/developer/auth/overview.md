# Authentication – Developer Overview

## Architecture

The authentication system follows the pluggable provider pattern (spec 5.2).
The active provider is selected via `Features:Authentication:Provider` in
`appsettings.json`.

### Key Classes

| Class | Project | Responsibility |
|---|---|---|
| `IAuthProvider` | Core | Authentication contract |
| `IGroupProvider` | Core | External group resolution contract |
| `LocalAuthProvider` | Infrastructure | DB-based username/password auth |
| `NullAuthProvider` | Infrastructure | Stub when auth is disabled |
| `NullGroupProvider` | Infrastructure | No external groups (Phase 1) |
| `PasswordService` | Infrastructure | Password policy + change logic |
| `AdminBootstrapService` | Infrastructure | Creates initial admin on first run |
| `CookieSessionAuthHandler` | Api | ASP.NET Core auth handler for session cookies |
| `MustChangePasswordMiddleware` | Api | Blocks requests when password change is required |
| `AuthController` | Api | Login, logout, me, change-password endpoints |

### Data Flow – Login

1. `POST /api/v1/auth/login` with `{ username, password }`
2. `AuthController` delegates to `IAuthProvider.AuthenticateAsync`
3. `LocalAuthProvider` looks up `ExternalIdentity` (ProviderType=Local)
4. Verifies password via `PasswordHasher<User>` (PBKDF2, per-user salt)
5. Failed attempt increments `FailedAttempts`; lockout after threshold
6. Success sets `HttpOnly`+`Secure`+`SameSite=Lax` cookie
7. Subsequent requests authenticated via `CookieSessionAuthHandler`

### Data Flow – Password Change

1. `POST /api/v1/auth/change-password` with `{ currentPassword, newPassword }`
2. `PasswordService` verifies current password, validates policy, updates hash
3. Clears `MustChangePassword` flag

## Database Tables

- `Users` – core user record
- `ExternalIdentities` – links users to auth providers
- `LocalCredentials` – password hash, lockout state (Local provider only)
- `Roles` – system roles (Admin, Developer, ApplicationOwner, ReadOnly)
- `UserRoles` – user-to-role assignments with grant source
- `Permissions`, `RolePermissions` – granular permission definitions
- `GroupRoleMappings` – external group to role mappings (future phases)
