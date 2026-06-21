# SoftwareInventory – Rendszerspecifikáció v1.0

**Dokumentum állapota:** Jóváhagyott tervezet
**Dátum:** 2026-06-18
**Verzió:** 1.0

---

## Tartalomjegyzék

1. [Projekt áttekintés](#1-projekt-áttekintés)
2. [Alapelvek és tervezési döntések](#2-alapelvek-és-tervezési-döntések)
3. [Technológiai stack](#3-technológiai-stack)
4. [Git-workflow és fejlesztési folyamat](#4-git-workflow-és-fejlesztési-folyamat)
5. [Feature flag és provider rendszer](#5-feature-flag-és-provider-rendszer)
6. [Adatmodell – EF Core Code-First](#6-adatmodell--ef-core-code-first)
7. [Authentikáció](#7-authentikáció)
8. [Jogosultságkezelés (RBAC)](#8-jogosultságkezelés-rbac)
9. [Alkalmazás-katalógus](#9-alkalmazás-katalógus)
10. [Dokumentáció-kezelés](#10-dokumentáció-kezelés)
11. [Keresés](#11-keresés)
12. [REST API](#12-rest-api) (beleértve a 12.4 MCP szerver – AI integráció szakaszt)
13. [Frontend](#13-frontend)
14. [Dokumentációs követelmények és Definition of Done](#14-dokumentációs-követelmények-és-definition-of-done)
15. [Infrastruktúra](#15-infrastruktúra)
16. [Biztonsági követelmények](#16-biztonsági-követelmények)
17. [Fejlesztési ütemterv](#17-fejlesztési-ütemterv)
18. [Glosszárium](#18-glosszárium)

---

## 1. Projekt áttekintés

### 1.1 Célkitűzés

A SoftwareInventory egy vállalati belső portál, amely egységes nyilvántartást biztosít a szervezeten belül fejlesztett szoftveralkalmazásokról. A rendszer célja, hogy bármely munkatárs – jogosultsági szintjétől függően – naprakész, kereshető információhoz jusson az egyes alkalmazásokról: elérhetőségükről, felelőseikről, dokumentációjukról és technikai részleteikről.

### 1.2 Célközönség

| Szerepkör            | Leírás                                                                                                               |
| -------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Felhasználók**     | Az alkalmazásokat igénybe vevő belső munkatársak (nem fejlesztők); alkalmazások keresése, user dokumentáció olvasása |
| **Fejlesztők**       | Az egyes alkalmazásokat fejlesztő és üzemeltető csapattagok; teljes katalógus-CRUD, developer doksi                  |
| **Adminisztrátorok** | A rendszer jogosultságait és konfigurációját kezelő személyek; AD csoport–szerep leképzések, felhasználókezelés      |

### 1.3 Hatókör – v1.0

- Alkalmazás-katalógus kezelése (CRUD, metaadatok, elérhetőségek, kapcsolattartók)
- Dedikált forráskód-link (Git / Azure DevOps) és web-környezet linkek (production, teszt) alkalmazásonként
- Dokumentáció-kezelés (felhasználói, fejlesztői és üzemeltetési doksik elkülönítve) beépített Markdown szerkesztővel
- Hitelesítés: AD/Kerberos SSO, bővíthető provider rendszerrel
- Jogosultságkezelés: AD csoport alapú RBAC
- Teljes szöveges keresés (bővíthető, PostgreSQL FTS-sel indulva)
- Admin felület: csoport–szerep leképzések, felhasználói szerepkörök kezelése
- Audit log: ki mit mikor módosított

### 1.4 Hatókörön kívül – v1.0

- OAuth 2.0 / OIDC provider (v2.0-ban tervezett)
- Okta SSO provider (v2.0-ban tervezett)
- Elasticsearch keresési provider (v2.0-ban tervezett)
- Email/Teams értesítések (v2.0-ban tervezett)
- CI/CD pipeline integráció

---

## 2. Alapelvek és tervezési döntések

### 2.1 Pluggable architektúra (P1)

**Alapelv:** Minden funkció mögé interfészt és DI-alapú provider regisztrációt kell helyezni. A konkrét implementációra soha nem hivatkozhat közvetlenül az üzleti logika.

**Következmény:** Bármelyik provider konfigurációs módosítással lecserélhető, forráskód-módosítás nélkül. A nem kívánt funkciók `NullProvider`-rel tilthatók le, miközben az alkalmazás többi része zavartalanul működik.

### 2.2 Feature flag minden funkcióra (P2)

Minden opcionális funkció rendelkezik egy `Enabled: true/false` konfigurációs kapcsolóval. A kikapcsolt funkció:

- API szinten: `501 Not Implemented` választ ad
- Frontend szinten: nem jeleníti meg az adott UI elemet (nem hibázik)
- DI szinten: `NullProvider` van injektálva

### 2.3 Documentation-First fejlesztés (P3)

A dokumentáció minden PR kötelező része, nem utólagos feladat. Hiányzó dokumentáció a PR elutasításának oka. Minden feature három szintű doksit igényel:

- **Developer doc** – architektúra, konfiguráció, debug
- **User doc** – felhasználói leírás, hogyan érhető el
- **Ops doc** – environment változók, K8s secretek, rollback

### 2.4 Code-First adatmodell (P4)

Az adatbázis-séma kizárólag EF Core migrációkon keresztül módosítható. Közvetlen DDL futtatás nem megengedett. Minden migrációhoz PostgreSQL DDL szkript is generálódik (az `ef-migration-postgresql` skill segítségével) az auditálhatóság és a manuális visszaállítás érdekében.

### 2.5 Konténer-natív, stateless API (P5)

Az alkalmazás stateless API szerverként fut – az állapotot kizárólag az adatbázis és a Kerberos/LDAP infrastruktúra tartja. Credentials és secretek soha nem kerülnek az image-be; kizárólag Kubernetes Secret-ből vagy Docker volume-ból töltődnek be.

---

## 3. Technológiai stack

### 3.1 Stack összefoglalása

| Komponens            | Választott technológia            | Indoklás                                                                                                    |
| -------------------- | --------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| **Backend**          | ASP.NET Core .NET 9               | Natív cross-platform Kerberos/SPNEGO (`Negotiate` middleware), legjobb Linux-support a GSSAPI stackek közül |
| **ORM / migráció**   | Entity Framework Core 9 + Npgsql  | Code-First, PostgreSQL provider, cserélhető MSSQL-re minimális módosítással                                 |
| **Adatbázis**        | PostgreSQL 16                     | Nyílt forráskódú, kiváló Docker/K8s support, beépített Full-Text Search, licencköltség nélküli production   |
| **Frontend**         | React 19 + TypeScript + Vite      | Gazdag markdown-rendering ökoszisztéma (react-markdown, rehype), aktív közösség                             |
| **API dokumentáció** | OpenAPI / Swashbuckle + Scalar UI | Automatikusan frissülő, interaktív explorer                                                                 |
| **Feature flags**    | Microsoft.FeatureManagement 4.x   | Hivatalos, konfigurációból vezérelt, IFeatureManager DI                                                     |
| **Logging**          | Serilog → Seq (vagy stdout)       | Strukturált logok, sink-ek pluggable-ek                                                                     |
| **Konténer**         | Docker + Kubernetes               | On-prem futtató platform                                                                                    |

### 3.2 MS SQL kompatibilitás

Az EF Core provider csere minimális módosítással elvégezhető:

```
Npgsql.EntityFrameworkCore.PostgreSQL → Microsoft.EntityFrameworkCore.SqlServer
```

Az üzleti logika és a migrációs C# kód változatlan marad; csak a provider csomag és a connection string változik. Ha a jövőben visszatérés szükséges MS SQL-re, a meglévő `ef-migration-sql` skill (T-SQL generálás) azonnal újra alkalmazható.

### 3.3 ADR-001 – Stack-választás

**Kontextus:** Linux konténerben futó alkalmazás, AD/Kerberos SSO böngészőből automatikusan, csapat C#/.NET tapasztalattal.
**Döntés:** ASP.NET Core .NET 9 + PostgreSQL.
**Elutasított alternatívák:** Java/Spring Security Kerberos (elfogadható, de a csapat C# kompetenciáját pazarlná); Python (GSSAPI wrapper kevésbé karbantartott production-szinten); Node.js (Kerberos support leggyengébb a szóba jövők közül).
**Következmény:** A Docker image-be `libgssapi-krb5-2` vagy `krb5-user` telepítendő. A csoporttagságot LDAP-lekérdezés adja, nem a Kerberos ticket (Linux limitáció).

### 3.4 ADR-002 – PostgreSQL vs MS SQL

**Döntés:** PostgreSQL 16 az elsődleges adatbázis.
**Indoklás:** Production licencköltség nélküli; natív Linux container operátor ekoszisztéma (CloudNativePG); FTS beépített, nem igényel külön konfigurációt; K8s-natív backupok és HA operátorok aktívan fejlesztve.
**Kockázat és mérséklés:** A csapat MS SQL tapasztalattal rendelkezik. Az EF Core provider-csere egyetlen csomagcsere – az adatbázis technológia cserélhető, ha üzemeltetési igény merül fel.

---

## 4. Git-workflow és fejlesztési folyamat

### 4.1 Ágstruktúra

```
main              ← production (protected, kizárólag PR-ból)
└─ test           ← staging/integrációs (protected, kizárólag PR-ból)
   ├─ feature/[feature-neve]     ← új funkció fejlesztése
   ├─ fix/[hibajegy-azonosító]   ← hibajavítás
   └─ docs/[téma]                ← kizárólag dokumentáció
```

**Szabályok:**

- `main` és `test` ágra közvetlen push tiltott
- `feature/*` ágak mindig `test`-ről ágaznak el (nem `main`-ről)
- Minden PR-hoz legalább 1 code review szükséges
- CI (build + unit test) zöldnek kell lennie merge előtt

### 4.2 Commit üzenet konvenciók (Conventional Commits)

```
<típus>(<hatókör>): <leírás>

Típusok : feat, fix, docs, refactor, test, chore, ci
Hatókörök: auth, catalog, search, docs, rbac, infra, api, frontend, admin

Példák:
feat(auth): implement Kerberos SPNEGO provider
feat(auth): add LDAP group sync with 15min cache
fix(catalog): null reference on missing owner team
docs(search): add PostgreSQL FTS configuration guide
refactor(rbac): extract GroupRoleMapper to separate service
test(catalog): add integration tests for application CRUD
chore(infra): add krb5-user to Docker base image
```

### 4.3 Pull Request folyamat

1. `feature/[nev]` ág létrehozása `test`-ről
2. Fejlesztés iteratív commitokkal
3. Unit tesztek írása az üzleti logikához
4. Dokumentáció frissítése (developer + user + ops szint, ha érintett)
5. PR nyitása `test`-re
6. Code review (min. 1 approval)
7. Definition of Done ellenőrzése (ld. 14. fejezet)
8. Merge `test`-re → automatikus deploy staging-re
9. Manuális elfogadási teszt staging-en
10. PR `test` → `main` → production deploy

### 4.4 Naming konvenciók – branch-ek

| Branch típus | Minta                             | Példa                            |
| ------------ | --------------------------------- | -------------------------------- |
| Új funkció   | `feature/[domain]-[rövid-leírás]` | `feature/auth-kerberos-provider` |
| Hibajavítás  | `fix/[jegy-szám]-[rövid-leírás]`  | `fix/42-null-owner-team`         |
| Dokumentáció | `docs/[téma]`                     | `docs/kerberos-setup-guide`      |
| Refaktorálás | `refactor/[hatókör]-[leírás]`     | `refactor/rbac-group-mapper`     |

---

## 5. Feature flag és provider rendszer

### 5.1 Konfigurációs struktúra (`appsettings.json`)

```json
{
  "Features": {
    "Authentication": {
      "Enabled": true,
      "Provider": "Kerberos"
    },
    "Authorization": {
      "Enabled": true
    },
    "ApplicationCatalog": {
      "Enabled": true
    },
    "Documentation": {
      "Enabled": true,
      "AllowDeveloperDocs": true,
      "AllowOperationsDocs": true
    },
    "Search": {
      "Enabled": true,
      "Provider": "PostgresFts"
    },
    "AuditLog": {
      "Enabled": true,
      "Provider": "Database",
      "RetentionDays": 90
    },
    "Notifications": {
      "Enabled": false,
      "Provider": "Null"
    },
    "Mcp": {
      "Enabled": false,
      "Transport": "StreamableHttp",
      "Stateless": true,
      "RequireAuthentication": true,
      "DefaultRole": "ReadOnly",
      "ExposeWriteTools": false
    }
  }
}
```

### 5.2 Provider regisztrációs minta (kötelező minden pluggable komponenshez)

Az alábbi minta **minden** pluggable komponens esetén egységesen követendő:

```csharp
// 1. Interfész – a core projektben
public interface ISearchProvider
{
    bool IsAvailable { get; }
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
}

// 2. Null provider – feature kikapcsolt állapothoz
public sealed class NullSearchProvider : ISearchProvider
{
    public bool IsAvailable => false;

    public Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct)
        => Task.FromResult(SearchResult.Unavailable());
}

// 3. Éles implementáció
public sealed class PostgresFtsSearchProvider : ISearchProvider
{
    public bool IsAvailable => true;
    // ... tényleges implementáció
}

// 4. DI regisztrációs extension – az infrastruktúra projektben
public static class SearchServiceExtensions
{
    public static IServiceCollection AddSearchProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("Features:Search");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<ISearchProvider, NullSearchProvider>();
            return services;
        }

        var provider = section.GetValue<string>("Provider")
            ?? throw new InvalidOperationException("Features:Search:Provider is required when search is enabled.");

        return provider switch
        {
            "PostgresFts"     => services.AddScoped<ISearchProvider, PostgresFtsSearchProvider>(),
            "Elasticsearch"   => services.AddScoped<ISearchProvider, ElasticsearchSearchProvider>(),
            _                 => throw new InvalidOperationException($"Unknown search provider: '{provider}'")
        };
    }
}

// 5. Program.cs regisztráció
builder.Services.AddSearchProvider(builder.Configuration);
```

### 5.3 Pluggable komponensek – teljes lista

| Funkció             | Interfész               | Provider: 1. fázis          | Provider: tervezett                                      |
| ------------------- | ----------------------- | --------------------------- | -------------------------------------------------------- |
| Authentikáció       | `IAuthProvider`         | `KerberosAuthProvider`      | `OAuthProvider`, `OktaProvider`, `LocalProvider`         |
| Csoportleolvasás    | `IGroupProvider`        | `LdapGroupProvider`         | `OktaGroupProvider`, `ClaimsGroupProvider`               |
| Keresés             | `ISearchProvider`       | `PostgresFtsSearchProvider` | `ElasticsearchSearchProvider`                            |
| Audit log           | `IAuditProvider`        | `DatabaseAuditProvider`     | `SeqAuditProvider`, `NullAuditProvider`                  |
| Dokumentáció-tároló | `IDocumentStore`        | `DatabaseDocumentStore`     | `GitDocumentStore`                                       |
| Értesítés           | `INotificationProvider` | `NullNotificationProvider`  | `EmailNotificationProvider`, `TeamsNotificationProvider` |
| MCP eszköztár       | `IMcpToolset`           | `NullMcpToolset`            | `CatalogMcpToolset`, `DocumentationMcpToolset`           |

### 5.4 Feature API végpont

```
GET /api/v1/config/features
```

Visszaadja az aktív feature-öket (publikus, auth nélküli végpont). A frontend ebből dönti el, mit jelenítsen meg.

```json
{
  "search": { "enabled": true, "provider": "PostgresFts" },
  "documentation": { "enabled": true, "developerDocs": true },
  "auditLog": { "enabled": true }
}
```

---

## 6. Adatmodell – EF Core Code-First

### 6.1 Alkalmazás-domain entitások

```csharp
// src/AppInventory.Core/Entities/Application.cs
public class Application
{
    public int Id { get; set; }
    public required string Name { get; set; }              // unique
    public required string ShortDescription { get; set; }
    public string? DetailedDescription { get; set; }
    public ApplicationStatus Status { get; set; }
    public ApplicationType Type { get; set; }
    public required string OwnerTeam { get; set; }

    // Forráskód – dedikált mező. A típus megkülönbözteti a Git és az Azure DevOps
    // repókat (UI ikon, link-validáció), a RepositoryUrl tárolja a tényleges címet.
    public SourceControlType SourceControl { get; set; } = SourceControlType.None;
    public string? RepositoryUrl { get; set; }

    public string? WikiUrl { get; set; }
    public bool IsDeleted { get; set; } = false;           // soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<ApplicationEnvironment> Environments { get; set; } = [];
    public ICollection<ApplicationContact> Contacts { get; set; } = [];
    public ICollection<Documentation> Documentations { get; set; } = [];
    public ICollection<ApplicationTag> Tags { get; set; } = [];
}

public enum ApplicationStatus { Active, Maintenance, Deprecated, Retired }
public enum ApplicationType   { WebApp, ApiService, Library, BatchJob, MobileApp, Other }

// Forráskód-kezelő rendszer típusa a dedikált RepositoryUrl mezőhöz
public enum SourceControlType { None, Git, AzureDevOps }

public class ApplicationEnvironment
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public EnvironmentType Type { get; set; }
    public required string Url { get; set; }   // pl. production / teszt környezet web-címe
    public string? Notes { get; set; }
    // Ha false, csak Developer/Admin látja (nem-publikus staging URL-ek)
    public bool IsPublic { get; set; } = true;
    public Application Application { get; set; } = null!;
}

public enum EnvironmentType { Development, Test, UAT, Production }

public class ApplicationContact
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public ContactType Type { get; set; }
    public required string Value { get; set; }
    public string? Label { get; set; }
    public Application Application { get; set; } = null!;
}

public enum ContactType { Email, Slack, Teams, SupportUrl, Phone }

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }   // unique, lowercase
    public string? Color { get; set; }          // hex kód, UI-ban megjelenik
    public ICollection<ApplicationTag> Applications { get; set; } = [];
}

public class ApplicationTag
{
    public int ApplicationId { get; set; }
    public int TagId { get; set; }
    public Application Application { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
```

### 6.2 Dokumentáció entitások

```csharp
// src/AppInventory.Core/Entities/Documentation.cs
public class Documentation
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }    // Markdown
    public DocumentationType Type { get; set; }
    public DocumentationStatus Status { get; set; } = DocumentationStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? AuthorUserId { get; set; }
    public Application Application { get; set; } = null!;
    public User? Author { get; set; }
    public ICollection<DocumentationHistory> History { get; set; } = [];
}

public enum DocumentationType
{
    User,        // Minden authentikált user látja
    Developer,   // Developer + Admin látja
    Operations   // Admin látja (bővíthető)
}

public enum DocumentationStatus { Draft, Published, Archived }

public class DocumentationHistory
{
    public int Id { get; set; }
    public int DocumentationId { get; set; }
    public required string Content { get; set; }
    public int Version { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int? ArchivedByUserId { get; set; }
    public Documentation Documentation { get; set; } = null!;
}
```

### 6.3 Auth és RBAC entitások

```csharp
// src/AppInventory.Core/Entities/User.cs
public class User
{
    public int Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class ExternalIdentity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AuthProviderType ProviderType { get; set; }
    public required string ExternalId { get; set; }         // sAMAccountName (AD)
    public string? ExternalGroupsJson { get; set; }         // cachedelt csoportlista
    public DateTime LastSyncedAt { get; set; }
    public User User { get; set; } = null!;
}

public enum AuthProviderType { ActiveDirectory, OAuth, Okta, Local }

public class Role
{
    public int Id { get; set; }
    public required string Name { get; set; }   // Admin, Developer, ApplicationOwner, ReadOnly
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }       // rendszer által kezelt, nem törölhető
    public ICollection<RolePermission> Permissions { get; set; } = [];
    public ICollection<UserRole> UserAssignments { get; set; } = [];
    public ICollection<GroupRoleMapping> GroupMappings { get; set; } = [];
}

public class Permission
{
    public int Id { get; set; }
    public required string Name { get; set; }        // "application:read", "docs:developer:read"
    public required string ResourceType { get; set; }
    public required string Action { get; set; }
    public ICollection<RolePermission> Roles { get; set; } = [];
}

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int? GrantedByUserId { get; set; }
    public DateTime GrantedAt { get; set; }
    public RoleGrantSource Source { get; set; }    // Manual | GroupMapping
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public enum RoleGrantSource { Manual, GroupMapping }

public class GroupRoleMapping
{
    public int Id { get; set; }
    public AuthProviderType ProviderType { get; set; }
    public required string ExternalGroupRef { get; set; }  // AD DN vagy csoportnév
    public int RoleId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Role Role { get; set; } = null!;
}
```

### 6.4 Audit entitás

```csharp
// src/AppInventory.Core/Entities/AuditLog.cs
public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public required string Action { get; set; }         // Created, Updated, Deleted, Viewed
    public required string ResourceType { get; set; }   // Application, Documentation, User, Role
    public required string ResourceId { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 6.5 Migration stratégia és naming konvenció

```
Migráció neve: {YYYYMMddHHmm}_{Domain}_{Leírás}

Példák:
202506181200_Auth_InitialSchema
202506181400_Catalog_ApplicationAndEnvironment
202506190900_Docs_DocumentationAndHistory
202506200800_Rbac_RolesAndPermissions
```

- Minden migráció `Up()` és `Down()` metódust egyaránt tartalmaz
- Minden migrációhoz az `ef-migration-postgresql` skill PostgreSQL DDL szkriptet generál a `sql/migrations/` mappába
- Production deploy előtt a DDL szkriptet code review keretében átvizsgálják

### 6.6 Projekt-struktúra (Backend)

```
src/
├── AppInventory.Core/          # Entitások, interfészek, domain logika (nincs DI függőség)
│   ├── Entities/
│   ├── Interfaces/             # IAuthProvider, ISearchProvider, stb.
│   └── ValueObjects/
├── AppInventory.Infrastructure/ # EF Core, provider implementációk
│   ├── Data/                   # DbContext, migrációk
│   │   └── Migrations/
│   ├── Auth/                   # KerberosAuthProvider, LdapGroupProvider
│   ├── Search/                 # PostgresFtsSearchProvider
│   ├── Mcp/                    # CatalogMcpToolset, DocumentationMcpToolset, NullMcpToolset
│   └── Audit/                  # DatabaseAuditProvider
├── AppInventory.Api/           # ASP.NET Core projekt, controllerek, DI összerakás
│   ├── Controllers/
│   ├── Middleware/
│   └── Extensions/             # AddSearchProvider, AddAuthProvider, stb.
└── AppInventory.Tests/
    ├── Unit/
    └── Integration/

sql/
└── migrations/                 # Generált DDL szkriptek migrációnként

docs/
├── developer/
├── user/
├── ops/
└── adr/

frontend/
```

---

## 7. Authentikáció

### 7.1 IAuthProvider interfész

```csharp
// src/AppInventory.Core/Interfaces/IAuthProvider.cs
public interface IAuthProvider
{
    string ProviderType { get; }

    // Kerberos/Negotiate esetén ez a middleware trigger – más providernél lehet redirect URL
    Task<AuthenticateResult> AuthenticateAsync(HttpContext context, CancellationToken ct = default);

    // Provider-specifikus adatokból egységes ExternalIdentity-t ad vissza
    Task<ExternalIdentityDto?> GetExternalIdentityAsync(HttpContext context, CancellationToken ct = default);
}

public record AuthenticateResult(bool Success, string? ErrorMessage = null, string? RedirectUrl = null);
public record ExternalIdentityDto(AuthProviderType ProviderType, string ExternalId, string DisplayName, string Email);
```

### 7.2 AD/Kerberos implementáció

**Konfigurációs séma:**

```json
{
  "Features": {
    "Authentication": { "Enabled": true, "Provider": "Kerberos" }
  },
  "Kerberos": {
    "ServiceAccountUpn": "svc-appinventory@CEGDOMAIN.LOCAL",
    "KeytabPath": "/run/secrets/appinventory.keytab",
    "Realm": "CEGDOMAIN.LOCAL"
  },
  "Ldap": {
    "Host": "dc01.cegdomain.local",
    "Port": 389,
    "UseLdaps": false,
    "BaseDn": "DC=cegdomain,DC=local",
    "ServiceAccountDn": "CN=svc-ldapread,OU=ServiceAccounts,DC=cegdomain,DC=local",
    "ServiceAccountPasswordPath": "/run/secrets/ldap-password",
    "UserSearchFilter": "(&(objectClass=user)(sAMAccountName={0}))",
    "GroupMembershipAttribute": "memberOf",
    "GroupCacheTtlMinutes": 15
  }
}
```

**Működési folyamat:**

```
1. Böngésző HTTP kérés → ASP.NET Core Negotiate middleware
2. Negotiate challenge (401) → böngésző elküldi a Kerberos ticketet
3. Keytab-alapú ticket-validálás → sAMAccountName kinyerése
4. LdapGroupProvider.GetGroupsAsync(sAMAccountName):
   a. Memória-cache ellenőrzése (15 perces TTL)
   b. Cache miss: LDAP bind service accounttal, memberOf lekérés
   c. Csoportlista cache-elés ExternalGroupsJson-ban
5. GroupRoleMapping táblán keresztül csoportok → belső szerepkörök
6. User rekord JIT provisioning: első belépéskor automatikusan létrejön
7. HTTP kontextus ClaimsPrincipal feltöltése szerepkörökkel
```

### 7.3 IGroupProvider interfész

```csharp
// src/AppInventory.Core/Interfaces/IGroupProvider.cs
public interface IGroupProvider
{
    Task<IReadOnlyList<string>> GetGroupsAsync(
        string externalUserId,
        AuthProviderType providerType,
        CancellationToken ct = default);
}

// LdapGroupProvider: LDAP bind → memberOf attribútum olvasás + cache
// OktaGroupProvider (jövőbeli): Okta Groups API + cache
// ClaimsGroupProvider (jövőbeli): JWT token claims-ből olvas
```

### 7.4 Jövőbeli providerek előkészítése

Az `IAuthProvider` és `IGroupProvider` interfészek tervezetten fogadják a jövőbeli implementációkat:

| Provider        | Auth flow                            | Csoport-forrás     |
| --------------- | ------------------------------------ | ------------------ |
| `OAuthProvider` | OIDC authorization code flow         | JWT `groups` claim |
| `OktaProvider`  | Okta OIDC                            | Okta Groups API    |
| `LocalProvider` | Username/password (fejlesztői célra) | Adatbázis          |

A `GroupRoleMapping.ProviderType` oszlop biztosítja, hogy különböző providerektől érkező csoportok eltérő leképzésekkel rendelkezzenek.

---

## 8. Jogosultságkezelés (RBAC)

### 8.1 Beépített rendszer-szerepkörök

| Szerepkör          | Leírás                                                                | IsSystemRole |
| ------------------ | --------------------------------------------------------------------- | ------------ |
| `Admin`            | Teljes hozzáférés; felhasználókezelés, csoportleképzések, minden CRUD | true         |
| `Developer`        | Alkalmazás CRUD + developer/ops doksi olvasás + írás                  | true         |
| `ApplicationOwner` | Saját alkalmazásai szerkesztése + user doksi írás                     | true         |
| `ReadOnly`         | Minden publikus tartalom olvasása (user doksi, katalógus)             | true         |

### 8.2 Jogosultsági mátrix

| Erőforrás / Akció              | ReadOnly | AppOwner | Developer | Admin |
| ------------------------------ | :------: | :------: | :-------: | :---: |
| Alkalmazás lista és olvasás    |    ✓     |    ✓     |     ✓     |   ✓   |
| Alkalmazás létrehozás          |    —     |    —     |     ✓     |   ✓   |
| Saját alkalmazás szerkesztés   |    —     |    ✓     |     ✓     |   ✓   |
| Bármely alkalmazás szerkesztés |    —     |    —     |     —     |   ✓   |
| Alkalmazás törlés (soft)       |    —     |    —     |     —     |   ✓   |
| Nem-publikus környezet URL-ek  |    —     |    ✓     |     ✓     |   ✓   |
| User doksi olvasás             |    ✓     |    ✓     |     ✓     |   ✓   |
| User doksi írás                |    —     |    ✓     |     ✓     |   ✓   |
| Developer doksi olvasás        |    —     |    —     |     ✓     |   ✓   |
| Developer doksi írás           |    —     |    —     |     ✓     |   ✓   |
| Operations doksi               |    —     |    —     |     —     |   ✓   |
| Audit log megtekintés          |    —     |    —     |     —     |   ✓   |
| GroupRoleMapping kezelés       |    —     |    —     |     —     |   ✓   |
| Felhasználókezelés             |    —     |    —     |     —     |   ✓   |

### 8.3 AD csoport–szerepkör leképzési konfiguráció

A `GroupRoleMapping` táblát az Admin felületen konfigurálják. Induló minta:

| AD csoport (DN)                              | Szerepkör          |
| -------------------------------------------- | ------------------ |
| `CN=IT-Developers,OU=Groups,DC=ceg,DC=local` | `Developer`        |
| `CN=IT-All,OU=Groups,DC=ceg,DC=local`        | `ReadOnly`         |
| `CN=IT-AppAdmin,OU=Groups,DC=ceg,DC=local`   | `Admin`            |
| `CN=IT-AppOwners,OU=Groups,DC=ceg,DC=local`  | `ApplicationOwner` |

### 8.4 Jogosultság kiértékelési sorrend

```
1. Authentikáció ellenőrzése        → 401 ha nem authentikált
2. Feature flag ellenőrzése         → 501 ha a funkció le van tiltva
3. Felhasználó aktív-e              → 403 ha IsActive = false
4. Szerepkör megléte                → 403 ha nincs megfelelő szerepkör
5. Erőforrás-szintű ellenőrzés      → 403 vagy engedélyezés
```

### 8.5 Jogosultság-szűrés query szinten

A Developer és Operations dokumentumok nem csak HTTP szinten, hanem EF Core query szinten is szűrtek, hogy az elérhetetlen tartalmak ne jelenjenek meg keresési eredményben sem:

```csharp
// Ha a user nem Developer/Admin, a query kiszűri a nem-User típusú doksikat
query = query.Where(d => d.Type == DocumentationType.User || userHasDeveloperAccess);
```

---

## 9. Alkalmazás-katalógus

### 9.1 Funkció leírás

Az alkalmazás-katalógus a rendszer magja. Tárolja és megjeleníti az összes belső fejlesztésű szoftvert metaadataival, elérhetőségeivel és kapcsolattartói adataival.

### 9.2 API végpontok

```
GET    /api/v1/applications                        → lista (lapozható, szűrhető)
GET    /api/v1/applications/{id}                   → részletes nézet
POST   /api/v1/applications                        → létrehozás [Developer, Admin]
PUT    /api/v1/applications/{id}                   → szerkesztés [ApplicationOwner, Developer, Admin]
DELETE /api/v1/applications/{id}                   → soft delete [Admin]

GET    /api/v1/applications/{id}/environments      → környezetek listája
POST   /api/v1/applications/{id}/environments      → [Developer, Admin]
PUT    /api/v1/applications/{id}/environments/{eid} → [Developer, Admin]
DELETE /api/v1/applications/{id}/environments/{eid} → [Developer, Admin]

GET    /api/v1/applications/{id}/contacts          → kapcsolattartók
POST   /api/v1/applications/{id}/contacts          → [Developer, Admin]
PUT    /api/v1/applications/{id}/contacts/{cid}    → [Developer, Admin]
DELETE /api/v1/applications/{id}/contacts/{cid}    → [Developer, Admin]

GET    /api/v1/tags                                → tag lista
POST   /api/v1/tags                                → [Admin]
DELETE /api/v1/tags/{id}                           → [Admin]
```

### 9.3 Szűrési és lapozási paraméterek (`GET /api/v1/applications`)

| Paraméter  | Típus               | Leírás                                                              |
| ---------- | ------------------- | ------------------------------------------------------------------- |
| `status`   | enum                | `active`, `maintenance`, `deprecated`, `retired`                    |
| `type`     | enum                | `webapp`, `apiservice`, `library`, `batchjob`, `mobileapp`, `other` |
| `team`     | string              | Részleges egyezés az `OwnerTeam` mezőn                              |
| `tag`      | string (többszörös) | Tag neve; AND logika több `tag` paraméter esetén                    |
| `q`        | string              | Átadja a Search providernek (ha engedélyezett)                      |
| `page`     | int                 | 1-től számozva, default: 1                                          |
| `pageSize` | int                 | Default: 20, max: 100                                               |
| `sort`     | string              | `name`, `createdAt`, `updatedAt`; prefix `-` csökkentő sorrendhez   |

### 9.4 Forráskód- és környezet-linkek

Minden alkalmazás dedikált mezőkben tárolja a fejlesztéshez és üzemeltetéshez tartozó hivatkozásokat. Ezek az alkalmazás részletes nézetében külön, jól látható blokkban jelennek meg.

**Forráskód (kötelezően ajánlott minden alkalmazásnál):**

- `SourceControl` (`Git` | `AzureDevOps` | `None`) – a repó típusa; az UI ez alapján jelenít meg ikont, és validálja a link sémáját
- `RepositoryUrl` – a forráskód-repó teljes URL-je (pl. `https://dev.azure.com/{org}/{project}/_git/{repo}` vagy `https://git.cegdomain.local/{group}/{repo}`)
- A link megnyitása új lapon (`target="_blank"`, `rel="noopener noreferrer"`) történik

**Web-környezet linkek (`WebApp` és `ApiService` típusnál):**

- A környezet-URL-ek az `ApplicationEnvironment` entitásban tárolódnak `EnvironmentType` szerint
- A részletes nézet a `Production` és `Test` környezet linkjét kiemelten jeleníti meg; a `Development`/`UAT` a teljes környezet-listában szerepel
- A `IsPublic = false` környezetek (pl. nem-publikus staging) csak `Developer`/`Admin` szerepkörnek jelennek meg (ld. 8.2 jogosultsági mátrix)
- Nem-web típusú alkalmazásnál (`Library`, `BatchJob`, `Other`) a környezet-link blokk üres marad, az UI nem hibázik

**Validáció:**

- `RepositoryUrl` és minden `ApplicationEnvironment.Url` csak `http`/`https` sémát fogad el; egyéb séma `400 Bad Request` (RFC 7807)
- A `SourceControl = AzureDevOps`/`Git` választásnál a `RepositoryUrl` kötelező; `None` esetén üres maradhat

---

## 10. Dokumentáció-kezelés

### 10.1 Dokumentáció típusok és láthatóság

| Típus        | Leírás                                             | Jogosultság              |
| ------------ | -------------------------------------------------- | ------------------------ |
| `User`       | Végfelhasználóknak: funkciók, elérhetőség, kontakt | Minden authentikált user |
| `Developer`  | Fejlesztőknek: API, konfig, deploy, ADR, debug     | Developer + Admin        |
| `Operations` | Üzemeltetési runbook, riasztáskezelés              | Admin (bővíthető)        |

### 10.2 Markdown tárolás és renderelés

- A dokumentum törzse (`Content`) Markdown formátumban van az adatbázisban
- Frontend (megjelenítés): `react-markdown` + `rehype-highlight` (szintaxiskiemelő) + `remark-gfm` (táblázatok, checkboxok)
- Frontend (szerkesztés): beépített Markdown editor a szerkesztő felületen (ld. 10.5)
- Maximális dokumentumméret: 500 KB/dokumentum
- Képek: külső URL-ként hivatkozhatók (nem tárol binárist a rendszer)

### 10.3 Verzióhistory

- Minden mentéskor `Version` mező inkrementálódik
- Az előző tartalom a `DocumentationHistory` táblában megmarad
- Az előző verziók 90 napig olvashatók, utána archivált állapotba kerülnek
- Diff view (két verzió összehasonlítása) a frontend feature-ek közé tartozik

### 10.4 API végpontok

```
GET    /api/v1/applications/{id}/docs                     → lista (type szűrő, jogosultság alapján)
GET    /api/v1/applications/{id}/docs/{docId}             → tartalom
POST   /api/v1/applications/{id}/docs                     → létrehozás [Developer, Admin]
PUT    /api/v1/applications/{id}/docs/{docId}             → szerkesztés [Developer, Admin]
PATCH  /api/v1/applications/{id}/docs/{docId}/status      → Draft/Published/Archived [Developer, Admin]
DELETE /api/v1/applications/{id}/docs/{docId}             → archivál [Admin]
GET    /api/v1/applications/{id}/docs/{docId}/history     → verzióhistory [Developer, Admin]
GET    /api/v1/applications/{id}/docs/{docId}/history/{v} → adott verzió tartalma [Developer, Admin]
```

### 10.5 Markdown szerkesztő (UI)

A dokumentáció szerkesztését beépített Markdown editor könnyíti meg, így a felhasználónak nem kell külön eszközben írnia a Markdownt.

**Könyvtár:** `@uiw/react-md-editor` – React 19-kompatibilis, GitHub-stílusú Markdown editor élő előnézettel, beépített toolbarral; a megjelenítés ugyanazt a `remark-gfm` plugin-láncot használja, mint az olvasó nézet, így a szerkesztés és a végleges renderelés egyezik (WYSIWYG-közeli élmény).

**Funkciók:**

- Toolbar a gyakori formázásokhoz (címsor, félkövér, dőlt, lista, link, kódblokk, táblázat)
- Élő, megosztott (split) előnézet, ugyanazzal a renderelővel, mint a 13.4 olvasó nézet
- Teljes képernyős (fullscreen) szerkesztő mód hosszabb dokumentumokhoz
- Karakterszámláló és az 500 KB-os limit (10.2) kliens oldali jelzése mentés előtt
- A szerkesztő a `DocumentationEdit` oldalon jelenik meg; a mentés a 10.4 `POST`/`PUT` végpontokat hívja
- Csak `Developer`/`Admin` szerepkörnek jelenik meg írásra (ld. 8.2); az olvasói nézet változatlanul a renderelt Markdownt mutatja

**Feature flag:** a szerkesztő a meglévő `Features:Documentation:Enabled` kapcsolóhoz kötött; kikapcsolt dokumentáció-funkció esetén az editor sem jelenik meg.

---

## 11. Keresés

### 11.1 ISearchProvider interfész és típusok

```csharp
// src/AppInventory.Core/Interfaces/ISearchProvider.cs
public record SearchQuery(
    string Term,
    IReadOnlyList<string>? ResourceTypes = null,  // "application", "documentation"
    IReadOnlyList<string>? Tags = null,
    int Page = 1,
    int PageSize = 20);

public record SearchResultItem(
    string ResourceType,
    int ResourceId,
    string Title,
    string Snippet,
    double Score);

public record SearchResult(
    IReadOnlyList<SearchResultItem> Items,
    int TotalCount,
    bool IsAvailable)
{
    public static SearchResult Unavailable()
        => new([], 0, IsAvailable: false);
}
```

### 11.2 PostgreSQL Full-Text Search implementáció

- `tsvector` oszlopok az `Application` (`Name || ShortDescription || OwnerTeam`) és `Documentation.Content` táblákon
- GIN indexek a `tsvector` oszlopokon
- Alapértelmezett language config: `'hungarian'` (konfigurálható `'english'`-re is)
- Relevancia-rangsor: `ts_rank_cd` alapú
- **Jogosultság-szűrés a keresési eredményen is alkalmazva**: developer doksi csak Developer/Admin-nak jelenik meg
- Soft-deleted alkalmazások a keresési eredményből ki vannak szűrve

### 11.3 API végpont

```
GET /api/v1/search?q={term}&type=application&type=documentation&tag=java&page=1&pageSize=20
```

Ha a keresés ki van kapcsolva: `501 Not Implemented` + `{ "isAvailable": false }`.

### 11.4 Elasticsearch upgrade útvonal

Az `ElasticsearchSearchProvider` a jövőben az `Elastic.Clients.Elasticsearch` NuGet csomaggal implementálható. Az `ISearchProvider` interfész változatlan marad; a konfigurációban `"Provider": "PostgresFts"` → `"Provider": "Elasticsearch"` elegendő.

---

## 12. REST API

### 12.1 Alapelvek

- Verzionálás: URL prefix (`/api/v1/`, `/api/v2/`)
- Minden kérés `Content-Type: application/json`
- Authentication: Windows Negotiate (Kerberos) – böngésző automatikusan kezeli
- Hibakezelés: RFC 7807 Problem Details formátum
- Soft delete: a törölt erőforrások `DELETE` kérésre `IsDeleted = true`-ra állnak; `GET` végpontok alapból kiszűrik

### 12.2 Standard válaszformátumok

**Lista (lapozott):**

```json
{
  "items": [
    /* ... */
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

**Hibaválasz (RFC 7807):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Application with id 99 was not found.",
  "traceId": "00-abc123def456..."
}
```

**Feature kikapcsolt:**

```json
{
  "title": "Feature Not Available",
  "status": 501,
  "detail": "The search feature is currently disabled.",
  "feature": "Search"
}
```

### 12.3 OpenAPI dokumentáció

- Minden endpoint XML komment + `[ProducesResponseType]` attribútumok
- Scalar UI elérhető `/scalar` útvonalon (fejlesztési és staging környezetben)
- OpenAPI JSON letölthető `/openapi/v1.json` útvonalon
- Production-ban az OpenAPI UI letiltható (`Features:OpenApiUi:Enabled: false`)

### 12.4 MCP szerver – SoftwareInventory mint AI-eszköz

A rendszer önmagát **MCP (Model Context Protocol) szerverként** is publikálja, hogy AI-asszisztensek (pl. Claude Desktop, Claude Code, vállalati on-prem AI platform) közvetlenül hozzáadhassák eszközként. Az AI így természetes nyelven kérdezheti le az alkalmazás-katalógust, a dokumentációkat és a keresést — a REST API megkerülése nélküli, integrált élményt nyújtva.

**Tervezési alapelv:** Az MCP felület **nem** párhuzamos implementáció. A tool-ok ugyanazokat a domain-szolgáltatásokat (`ApplicationService`, `ISearchProvider`, `IDocumentStore`) hívják, mint a REST controllerek, és **ugyanazon az RBAC + feature-flag rétegen** mennek keresztül (P1, P2 alapelv).

#### 12.4.1 Technológia és transport

- **SDK:** hivatalos `ModelContextProtocol.AspNetCore` NuGet csomag
- **Transport:** Streamable HTTP (remote MCP server), a `/mcp` útvonalon publikálva
- **Stateless mód:** bekapcsolva (`options.Stateless = true`) — illeszkedik a stateless, horizontálisan skálázható K8s API elvhez (P5), nincs session-affinitás
- Az MCP endpoint az ASP.NET Core auth + RBAC pipeline mögött ül, nem anonim

#### 12.4.2 Regisztráció (Program.cs)

```csharp
// Csak ha a feature engedélyezett – egyébként az endpoint nincs map-elve (lásd 12.4.6)
builder.Services.AddMcpServerFeature(builder.Configuration);

// ...
app.MapMcpIfEnabled();   // belül: app.MapMcp("/mcp") ha Features:Mcp:Enabled
```

A feature-flag-tudatos extension a projekt DI-mintáját követi (5.2):

```csharp
public static IServiceCollection AddMcpServerFeature(
    this IServiceCollection services, IConfiguration configuration)
{
    var section = configuration.GetSection("Features:Mcp");
    if (!section.GetValue<bool>("Enabled"))
        return services;   // nincs MCP szerver regisztrálva

    services.AddMcpServer()
        .WithHttpTransport(o => o.Stateless = section.GetValue("Stateless", true))
        .WithTools<CatalogMcpToolset>()
        .WithTools<DocumentationMcpToolset>();

    return services;
}
```

#### 12.4.3 Publikált tool-ok (v1.0 – csak olvasás)

Az AI-nak kínált eszközök v1.0-ban kizárólag olvasási műveletek (`Features:Mcp:ExposeWriteTools = false`). A tool-metódusok DI-n keresztül kapják a domain-szolgáltatásokat, és minden hívásnál érvényesül a hívó identitásához tartozó RBAC-szűrés.

| Tool                       | Leírás                                                         | Mögöttes szolgáltatás       |
| -------------------------- | ------------------------------------------------------------- | --------------------------- |
| `search_applications`      | Teljes szöveges keresés a katalógusban és a doksikban         | `ISearchProvider`           |
| `list_applications`        | Szűrhető, lapozható alkalmazás-lista                          | `ApplicationService`        |
| `get_application`          | Részletes nézet (forráskód-link, környezetek, kapcsolattartók) | `ApplicationService`        |
| `get_application_environments` | Egy alkalmazás környezet-URL-jei (RBAC szerint szűrve)     | `ApplicationService`        |
| `list_documentation`       | Egy alkalmazás doksijai (típus + jogosultság szerint szűrve)   | `IDocumentStore`            |
| `get_documentation`        | Egy dokumentum Markdown tartalma                              | `IDocumentStore`            |

```csharp
[McpServerToolType]
public sealed class CatalogMcpToolset    // megvalósítja az IMcpToolset jelölő-szerepet
{
    [McpServerTool, Description("Full-text search across the application catalog and documentation.")]
    public async Task<SearchResult> SearchApplications(
        ISearchProvider search,                 // DI-n keresztül injektálva
        [Description("Search term")] string query,
        CancellationToken ct)
        => await search.SearchAsync(new SearchQuery(query), ct);
}
```

#### 12.4.4 Identitás és jogosultság

Mivel az MCP-kliens (AI) nem böngésző, a Kerberos/Negotiate flow itt nem alkalmazható közvetlenül. Az identitás-leképzés:

| Fázis | Mechanizmus                                                                                          |
| ----- | --------------------------------------------------------------------------------------------------- |
| v1.0  | **Service principal token** (bearer) a `RequireAuthentication` mögött; a `DefaultRole` (alapból `ReadOnly`) határozza meg a jogosultságot |
| v2.0  | **OAuth 2.1** (MCP-spec szerinti authorization) → a felhasználó nevében cselekvő AI, valós szerepkörrel |

A `WithHttpTransport(o => o.ConfigureSessionOptions = ...)` lehetővé teszi, hogy a publikált tool-készlet a hitelesített identitás szerepkörei alapján szűküljön (pl. developer doksi tool csak `Developer`/`Admin` tokennél). A tool-on belüli adat-szűrés mindig a query-szintű RBAC-ra (8.5) támaszkodik, így az AI soha nem kap olyan tartalmat, amit a hozzá rendelt szerepkör nem láthatna.

#### 12.4.5 Audit és biztonság

- Minden MCP tool-hívás auditálódik (`IAuditProvider`, `Action = "McpToolCall"`, `ResourceType = tool neve`)
- Az MCP endpoint a REST API-val azonos rate-limiting és HTTPS-kényszer alá esik (16.2)
- Írási tool-ok v1.0-ban tiltottak; ha a jövőben engedélyezve lesznek (`ExposeWriteTools = true`), `ApplicationOwner`/`Developer`/`Admin` token kötelező, és a művelet auditálódik
- A nem-publikus környezet-URL-ek és a developer/ops doksik csak a megfelelő szerepkörű tokennel térnek vissza

#### 12.4.6 Feature flag viselkedés

- `Features:Mcp:Enabled = false` (alapértelmezett): a `/mcp` endpoint **nincs** map-elve, az SDK nincs regisztrálva — nulla támadási felület és overhead
- `Features:Mcp:Enabled = true`: a `/mcp` Streamable HTTP endpoint aktív
- A `/api/v1/config/features` válasz tartalmazza: `"mcp": { "enabled": true }`

#### 12.4.7 AI-klienshez adás (felhasználói lépés)

Az AI-asszisztensbe a remote MCP szerver URL-jével és a service token-nel adható hozzá. Példa Claude Code / `.mcp.json` konfiguráció:

```json
{
  "mcpServers": {
    "software-inventory": {
      "type": "http",
      "url": "https://appinventory.cegdomain.local/mcp",
      "headers": { "Authorization": "Bearer ${SOFTWARE_INVENTORY_MCP_TOKEN}" }
    }
  }
}
```

A részletes telepítési és kliens-konfigurációs útmutató a `docs/integrations/mcp-server.md` fájlba kerül (ld. 14.2 fájl-struktúra), és új MCP tool felvétele az `mcp-tool-scaffold` skill-lel történik a projekt konvenciói szerint.

---

## 13. Frontend

### 13.1 Projekt-struktúra

```
frontend/
├── src/
│   ├── api/              # API client (fetch wrapper, TypeScript típusok)
│   ├── auth/             # Auth context, useAuth hook, Negotiate flow
│   ├── components/       # Újrahasználható UI komponensek
│   │   ├── ApplicationCard/
│   │   ├── DocumentationViewer/   # react-markdown alapú olvasó nézet
│   │   ├── MarkdownEditor/        # @uiw/react-md-editor wrapper (szerkesztés)
│   │   ├── SourceControlLink/     # forráskód link + típus-ikon (Git / Azure DevOps)
│   │   ├── EnvironmentLinks/      # production / teszt környezet linkek
│   │   ├── SearchBar/
│   │   └── TagBadge/
│   ├── features/         # Feature flag context, useFeature hook
│   ├── pages/            # Route-szintű oldalak
│   │   ├── ApplicationList/
│   │   ├── ApplicationDetail/
│   │   ├── DocumentationEdit/
│   │   └── AdminPanel/
│   └── hooks/            # useApplications, useSearch, useDocs
├── public/
└── vite.config.ts
```

### 13.2 Authentikáció a böngészőben

Kerberos (Negotiate) esetén az összes `fetch` kérést `credentials: 'include'` opcióval kell küldeni:

```typescript
const response = await fetch("/api/v1/applications", {
  credentials: "include", // kötelező: Negotiate ticket automatikus küldéséhez
  headers: { "Content-Type": "application/json" },
});
```

### 13.3 Feature flag kezelés

Az `api/v1/config/features` végpont eredményét a React context tárolja. A `useFeature` hook kondicionális renderelést tesz lehetővé:

```typescript
const { enabled } = useFeature('Search');
return enabled ? <SearchBar /> : null;
```

### 13.4 Markdown renderelés (Dokumentáció oldal)

```typescript
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';

<ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeHighlight]}>
  {documentation.content}
</ReactMarkdown>
```

### 13.5 Markdown szerkesztő (DocumentationEdit oldal)

A `DocumentationEdit` oldalon a `@uiw/react-md-editor` ad élő-előnézetes, toolbaros szerkesztőt. Az előnézet ugyanazt a `remark-gfm` láncot használja, mint a 13.4 olvasó nézet, így a szerkesztés és a végleges megjelenítés egyezik.

```typescript
import MDEditor from '@uiw/react-md-editor';
import remarkGfm from 'remark-gfm';

<MDEditor
  value={content}
  onChange={(val) => setContent(val ?? '')}
  height={500}
  previewOptions={{ remarkPlugins: [remarkGfm] }}
/>
```

### 13.6 Forráskód- és környezet-linkek megjelenítése

Az alkalmazás részletes nézete (`ApplicationDetail`) külön blokkban jeleníti meg a 9.4 szerinti linkeket:

```typescript
// Forráskód: típus-ikon + link (csak ha van RepositoryUrl)
{app.repositoryUrl && (
  <SourceControlLink type={app.sourceControl} url={app.repositoryUrl} />
)}

// Web-környezet linkek: Production és Test kiemelve, a többi a listában
{(app.type === 'WebApp' || app.type === 'ApiService') && (
  <EnvironmentLinks environments={app.environments} highlight={['Production', 'Test']} />
)}
```

---

## 14. Dokumentációs követelmények és Definition of Done

### 14.1 Definition of Done (DoD) – Feature PR checklist

**Kód minőség:**

- [ ] Unit tesztek az üzleti logikához (min. 80% lefedettség az **új** kódban)
- [ ] Integrációs teszt az API végpontokhoz (legalább happy path + 403 + 404)
- [ ] EF Core migráció + PostgreSQL DDL szkript generálva (ha DB változott)
- [ ] Feature flag (`Enabled: true/false`) implementálva
- [ ] `NullProvider` implementálva (ha pluggable komponens)

**Fejlesztői dokumentáció** (`docs/developer/{feature}/`):

- [ ] `overview.md`: architektúra döntés, főbb osztályok és felelősségeik
- [ ] `configuration.md`: összes konfigurációs kulcs, típus, alapértelmezett értékek, példák
- [ ] `providers.md`: elérhető providerek leírása, csere menete
- [ ] `troubleshooting.md`: ismert hibák és megoldásaik

**Felhasználói dokumentáció** (`docs/user/`):

- [ ] Megfelelő user doc fájl létezik vagy frissítve van
- [ ] Tartalmaz: mit csinál a funkció, hogyan érhető el, képernyőképek ha releváns
- [ ] **Nem** tartalmaz technikai részleteket (adatbázis, konfig, deployment)

**Ops dokumentáció** (`docs/ops/`):

- [ ] Ha konfiguráció vagy secret változott, frissítve van
- [ ] Tartalmaz: environment variable-ok, K8s Secret-ek neve, rollback eljárás

**OpenAPI:**

- [ ] Minden új/módosított endpoint XML kommenttel és `[ProducesResponseType]` attribútumokkal rendelkezik

### 14.2 Dokumentáció fájl-struktúra

```
docs/
├── developer/
│   ├── auth/
│   │   ├── overview.md
│   │   ├── kerberos-setup.md
│   │   ├── adding-new-provider.md
│   │   └── troubleshooting.md
│   ├── search/
│   ├── catalog/
│   ├── docs-module/
│   └── rbac/
├── user/
│   ├── getting-started.md
│   ├── applications.md
│   ├── search.md
│   └── documentation.md
├── ops/
│   ├── kerberos-keytab-setup.md
│   ├── deployment.md
│   ├── configuration-reference.md
│   └── troubleshooting.md
├── integrations/
│   └── mcp-server.md          # MCP szerver telepítés + AI-kliens konfiguráció
└── adr/
    ├── ADR-001-stack-selection.md
    ├── ADR-002-kerberos-ldap-auth.md
    ├── ADR-003-postgres-vs-mssql.md
    └── ADR-004-mcp-server-exposure.md
```

### 14.3 ADR sablon (`docs/adr/ADR-XXX-{name}.md`)

```markdown
# ADR-{szám}: {döntés rövid neve}

**Dátum:** YYYY-MM-DD
**Státusz:** Tervezet | Elfogadott | Visszavont | Felülvizsgálat alatt

## Kontextus

[Miért kellett dönteni? Mi volt a kényszer vagy a probléma?]

## Döntés

[Mit döntöttünk? Konkrétan, kerülve az indoklást ebben a szekcióban.]

## Következmények

[Milyen hatásai vannak? Mi válik könnyebbé, mi nehezebbé, milyen kockázatok jönnek?]

## Elutasított alternatívák

[Mik voltak a többi lehetőség, miért utasítottuk el?]
```

---

## 15. Infrastruktúra

### 15.1 Docker Compose (fejlesztői környezet)

```yaml
# docker-compose.dev.yml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: appinventory_dev
      POSTGRES_USER: appinventory
      POSTGRES_PASSWORD_FILE: /run/secrets/db-password
    ports:
      - "5432:5432"
    volumes:
      - postgres_dev_data:/var/lib/postgresql/data
    secrets: [db-password]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U appinventory"]
      interval: 5s
      retries: 5

  api:
    build:
      context: ./src/AppInventory.Api
      dockerfile: Dockerfile
    depends_on:
      db: { condition: service_healthy }
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: "Host=db;Database=appinventory_dev;Username=appinventory;Password=${DB_PASSWORD}"
      Features__Search__Enabled: "true"
      Features__Authentication__Provider: "Kerberos"
      Kerberos__KeytabPath: /run/secrets/appinventory.keytab
      Ldap__Host: ${LDAP_HOST}
      Ldap__ServiceAccountPasswordPath: /run/secrets/ldap-password
    volumes:
      - ./secrets/appinventory.keytab:/run/secrets/appinventory.keytab:ro
    secrets: [ldap-password]
    ports:
      - "5000:8080"

  frontend:
    build: ./frontend
    ports: ["3000:3000"]
    depends_on: [api]

volumes:
  postgres_dev_data:

secrets:
  db-password:
    file: ./secrets/db-password.txt
  ldap-password:
    file: ./secrets/ldap-password.txt
```

### 15.2 Dockerfile (API)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
# Kerberos Linux runtime függőség
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    krb5-user \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AppInventory.Api/AppInventory.Api.csproj", "AppInventory.Api/"]
COPY ["AppInventory.Core/AppInventory.Core.csproj", "AppInventory.Core/"]
COPY ["AppInventory.Infrastructure/AppInventory.Infrastructure.csproj", "AppInventory.Infrastructure/"]
RUN dotnet restore "AppInventory.Api/AppInventory.Api.csproj"
COPY . .
RUN dotnet build "AppInventory.Api/AppInventory.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AppInventory.Api/AppInventory.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppInventory.Api.dll"]
```

### 15.3 Kubernetes konfiguráció (production)

**Kötelező Secret-ek:**

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: appinventory-secrets
  namespace: appinventory
type: Opaque
data:
  keytab: <base64-encoded-keytab> # ktpass-szal generált
  ldap-password: <base64-encoded-password>
  db-password: <base64-encoded-password>
```

**Deployment fontos annotációk:**

```yaml
spec:
  # A hostname-nek egyeznie kell az SPN-nel
  hostname: appinventory
  subdomain: appinventory-svc
  containers:
    - name: api
      volumeMounts:
        - name: keytab
          mountPath: /run/secrets/appinventory.keytab
          subPath: keytab
          readOnly: true
  volumes:
    - name: keytab
      secret:
        secretName: appinventory-secrets
        items:
          - key: keytab
            path: keytab
```

### 15.4 Kerberos infrastruktúra előfeltételek

| Lépés                  | Felelős                 | Parancs / leírás                                                                                                                                                  |
| ---------------------- | ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SPN regisztráció       | AD adminisztrátor       | `setspn -A HTTP/appinventory.cegdomain.local svc-appinventory`                                                                                                    |
| Keytab generálás       | AD adminisztrátor       | `ktpass /princ HTTP/appinventory.cegdomain.local@CEGDOMAIN.LOCAL /mapuser svc-appinventory /crypto AES256-SHA1 /ptype KRB5_NT_PRINCIPAL /out appinventory.keytab` |
| DNS A rekord           | Hálózati adminisztrátor | `appinventory.cegdomain.local → K8s Ingress IP`                                                                                                                   |
| LDAP read-only account | AD adminisztrátor       | `svc-ldapread` service account: csak `Read` jogosultság a Users/Groups OU-ra                                                                                      |
| NTP szinkron           | Infrastruktúra          | K8s node-ok max. 5 perc eltéréssel a KDC-hez képest                                                                                                               |
| Kerberos konfig        | DevOps                  | `/etc/krb5.conf` a konténerben a tartomány KDC-jére mutat                                                                                                         |

---

## 16. Biztonsági követelmények

### 16.1 Authentikáció és munkamenet

- Minden API endpoint hitelesített kérést vár (kivétel: `/health`, `/api/v1/config/features`)
- A keytab fájl soha nem kerül az Docker image-be; kizárólag mount/Secret-en keresztül érhető el
- Az LDAP service account jelszava Kubernetes Secret-ben van; nem environment változóban
- Jövőbeli JWT tokeneket (OAuth/Okta) az ASP.NET Core Data Protection API-val kell védeni

### 16.2 Hálózat

- Az API szerver csak HTTPS-en érhető el (K8s Ingress TLS terminál)
- LDAP: LDAPS (636/TCP) vagy StartTLS; a `UseLdaps` config kapcsolóval állítható
- Kerberos forgalom (88/UDP+TCP) tűzfalon engedélyezett a DC felé
- A PostgreSQL csak a K8s belső hálózatáról érhető el

### 16.3 Adatvédelem

- Jelszavak, keytab-ok, service account hitelesítő adatok soha nem kerülnek logba
- Audit log GDPR szempontjából személyes adatot tartalmaz (user ID, IP); alapértelmezett megőrzés: 90 nap
- Developer doksi érzékeny technikai információt tartalmaz; jogosultság-ellenőrzés query szinten is alkalmazott

### 16.4 Secrets kezelési szintek

| Környezet        | Mechanizmus                                                      |
| ---------------- | ---------------------------------------------------------------- |
| Helyi fejlesztés | Docker secrets + helyi fájlok (`.gitignore`-ban)                 |
| Staging          | Kubernetes Secrets                                               |
| Production       | Kubernetes Secrets; jövőbeli bővítés: HashiCorp Vault CSI driver |

### 16.5 Dependency frissítési politika

- NuGet és npm csomagok havi rendszerességgel ellenőrzendők
- Biztonsági patch-ek 5 munkanapon belül alkalmazandók
- A `dotnet list package --vulnerable` és `npm audit` futtatása CI részévé kell tenni

---

## 17. Fejlesztési ütemterv

### Fázis 1 – Core (~6 hét)

- [ ] Projekt scaffolding: solution struktúra, Docker Compose, CI alap
- [ ] EF Core DbContext + base migration (User, Role, Permission, GroupRoleMapping)
- [ ] Feature flag + provider DI infrastruktúra
- [ ] Kerberos AuthProvider + LDAP GroupProvider
- [ ] Alapvető RBAC middleware
- [ ] Application + Environment + Contact + Tag CRUD API (forráskód-link + web-környezet linkek mezőkkel)
- [ ] React frontend: alkalmazás-lista és részletes nézet
- [ ] Dokumentáció: auth, catalog, rbac developer + user doc

### Fázis 2 – Dokumentáció és keresés (~4 hét)

- [ ] Documentation CRUD API (User, Developer, Operations típus)
- [ ] Hozzáférés-szűrés dokumentációknál (lekérdezés szinten is)
- [ ] DocumentationHistory + verzió-visszatekintés
- [ ] PostgreSQL FTS SearchProvider
- [ ] Keresési UI Reactban
- [ ] Dokumentáció Markdown editor UI (`@uiw/react-md-editor`, élő előnézet)

### Fázis 3 – Admin UI és audit (~4 hét)

- [ ] Admin panel: GroupRoleMapping CRUD
- [ ] Admin panel: User és UserRole management
- [ ] Audit log backend (DatabaseAuditProvider)
- [ ] Audit log megjelenítő (Admin)
- [ ] OpenAPI dokumentáció finalizálása + Scalar UI
- [ ] Tag management UI
- [ ] MCP szerver (read-only): `CatalogMcpToolset` + `DocumentationMcpToolset`, Streamable HTTP `/mcp`, service-token auth
- [ ] `docs/integrations/mcp-server.md` + ADR-004 (MCP exposure)

### Fázis 4 – Bővíthetőség (igény szerint)

- [ ] OAuth 2.0 / OIDC AuthProvider
- [ ] Okta SSO AuthProvider
- [ ] Elasticsearch SearchProvider
- [ ] Email / Teams NotificationProvider
- [ ] MCP szerver OAuth 2.1 auth (felhasználó nevében cselekvő AI) + opcionális írási tool-ok
- [ ] HashiCorp Vault CSI driver integráció

---

## 18. Glosszárium

| Fogalom              | Definíció                                                                                                                |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| **AuthProvider**     | Az authentikációs logikát absztraháló interfész; minden bejelentkezési mód (Kerberos, OAuth, Okta) ennek implementációja |
| **DoD**              | Definition of Done; a PR merge-elhetőségének feltételrendszere                                                           |
| **ExternalIdentity** | Egy felhasználó külső (provider-specifikus) azonosítóját tároló rekord                                                   |
| **GroupProvider**    | Az AD/Okta csoporttagság-lekérdezési logikát absztraháló interfész                                                       |
| **GroupRoleMapping** | AD/provider csoportot belső szerepkörhöz kötő konfigurációs rekord                                                       |
| **JIT provisioning** | Just-in-time felhasználó létrehozás; az első bejelentkezéskor automatikusan jön létre a User rekord                      |
| **Keytab**           | Kerberos service account titkosított hitelesítési adatait tartalmazó bináris fájl                                        |
| **MCP**              | Model Context Protocol; szabvány, amellyel AI-asszisztensek külső eszközökhöz/adatforrásokhoz kapcsolódnak. A rendszer MCP szerverként publikálja magát (12.4)        |
| **MCP toolset**      | Az MCP szerver által publikált, logikailag összetartozó tool-ok csoportja (pl. `CatalogMcpToolset`); a domain-szolgáltatásokra épül                                  |
| **LDAP**             | Lightweight Directory Access Protocol; az AD csoporttagság lekérdezéséhez használt protokoll                             |
| **Negotiate**        | ASP.NET Core middleware Kerberos/SPNEGO authentikációhoz; cross-platform .NET 9-en                                       |
| **NullProvider**     | Kikapcsolt funkció placeholder implementációja; `IsAvailable = false`, nem dob hibát                                     |
| **RBAC**             | Role-Based Access Control; szerepkör-alapú jogosultságkezelés                                                            |
| **Soft delete**      | Az `IsDeleted = true` flag beállítása törlés helyett; az adat megmarad, de nem jelenik meg                               |
| **SPNEGO**           | Simple and Protected GSSAPI Negotiation Mechanism; a böngésző–szerver Kerberos-handshake protokollja                     |
| **SPN**              | Service Principal Name; Kerberos-ban a service-t azonosító string (pl. `HTTP/hostname@REALM`)                            |
| **tsvector**         | PostgreSQL Full-Text Search indexstruktúrája; normalizált lexémák vektora                                                |
