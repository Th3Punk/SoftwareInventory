# CLAUDE.md – Projekt szintű szabályok

Referencia: `docs/specification.md` (v1.0)

## 1. Architektúra alapelvek

### P1 – Pluggable architektúra
- Minden funkció mögé **interfész** + DI-alapú provider regisztráció kerül
- Üzleti logika soha nem hivatkozhat közvetlenül konkrét implementációra
- Minden pluggable komponenshez kötelező: interfész (`Core/Interfaces/`), NullProvider, éles provider, DI extension (`Api/Extensions/`), config szekció, dokumentáció
- Soha ne generálj implementációt interfész és NullProvider nélkül

### P2 – Feature flag minden funkcióra
- Minden opcionális funkció rendelkezik `Enabled: true/false` konfigurációs kapcsolóval
- Kikapcsolt funkció: API → `501 Not Implemented`, Frontend → nem renderel, DI → `NullProvider`
- Feature flag konfiguráció helye: `appsettings.json` → `Features:{Domain}` szekció

### P3 – Documentation-First fejlesztés
- Dokumentáció minden PR kötelező része, hiánya a PR elutasításának oka
- Minden feature három szintű doksit igényel: Developer doc, User doc, Ops doc
- A `docs/developer/{domain}/` könyvtárban 4 fájl kötelező: `overview.md`, `configuration.md`, `providers.md`, `troubleshooting.md`

### P4 – Code-First adatmodell
- Adatbázis-séma kizárólag EF Core migrációkon keresztül módosítható
- Közvetlen DDL futtatás nem megengedett
- Minden migrációhoz PostgreSQL DDL szkript is generálódik a `sql/migrations/` mappába
- Migráció naming: `{YYYYMMddHHmm}_{Domain}_{Leírás}` (pl. `202506181200_Auth_InitialSchema`)
- Minden migráció `Up()` és `Down()` metódust egyaránt tartalmaz

### P5 – Stateless API
- Az alkalmazás stateless API szerverként fut
- Credentials és secretek soha nem kerülnek az image-be; kizárólag Kubernetes Secret-ből vagy Docker volume-ból töltődnek be

## 2. Projekt-struktúra és rétegzés

```
src/
├── AppInventory.Core/              # Entitások, interfészek – NINCS külső függőség
│   ├── Entities/
│   ├── Interfaces/
│   └── ValueObjects/
├── AppInventory.Infrastructure/    # EF Core, provider implementációk → referálja: Core
│   ├── Data/ (+ Migrations/)
│   ├── Auth/
│   ├── Search/
│   ├── Mcp/
│   └── Audit/
├── AppInventory.Api/               # ASP.NET Core host, DI → referálja: Core + Infrastructure
│   ├── Controllers/
│   ├── Middleware/
│   └── Extensions/
└── AppInventory.Tests/             # → referálja: mindhárom
    ├── Unit/
    └── Integration/
```

**Szabály:** Core projekt nem függhet semmilyen külső csomagtól (sem DI, sem EF Core). Interfészek és entitások élnek itt.

## 3. Technológiai stack

| Komponens        | Technológia                      |
| ---------------- | -------------------------------- |
| Backend          | ASP.NET Core .NET 9              |
| ORM              | Entity Framework Core 9 + Npgsql |
| Adatbázis        | PostgreSQL 16                    |
| Frontend         | React 19 + TypeScript + Vite     |
| API dokumentáció | OpenAPI + Scalar UI              |
| Feature flags    | Microsoft.FeatureManagement 4.x  |
| Logging          | Serilog                          |

## 4. Pluggable komponensek

| Funkció             | Interfész               | 1. fázis provider             | NullProvider kötelező |
| ------------------- | ----------------------- | ----------------------------- | --------------------- |
| Authentikáció       | `IAuthProvider`         | `LocalAuthProvider`           | igen                  |
| Csoportleolvasás    | `IGroupProvider`        | `NullGroupProvider`           | igen                  |
| Keresés             | `ISearchProvider`       | `PostgresFtsSearchProvider`   | igen                  |
| Audit log           | `IAuditProvider`        | `DatabaseAuditProvider`       | igen                  |
| Dokumentáció-tároló | `IDocumentStore`        | `DatabaseDocumentStore`       | igen                  |
| Értesítés           | `INotificationProvider` | `NullNotificationProvider`    | igen                  |
| MCP eszköztár       | `IMcpToolset`           | `NullMcpToolset`              | igen                  |

### Provider regisztrációs minta (spec 5.2)

Minden pluggable komponens DI regisztrációja:
1. Extension metódus: `Add{Domain}Provider(IServiceCollection, IConfiguration)`
2. `Features:{Domain}:Enabled` ellenőrzés → false esetén NullProvider
3. `Features:{Domain}:Provider` string → switch-case a provider-ekre
4. Ismeretlen provider → `InvalidOperationException`
5. `Program.cs`-ben: `builder.Services.Add{Domain}Provider(builder.Configuration);`

## 5. C# kódolási konvenciók

- File-scoped namespace (`csharp_style_namespace_declarations = file_scoped`)
- Private field: `_camelCase` prefix (`_logger`, `_dbContext`)
- Async metódusok: `Async` suffix kötelező
- Interfészek: `I` prefix
- Provider osztályok: `internal sealed`
- NullProvider: `IsAvailable => false`, soha nem dob kivételt
- `required` modifier a kötelező string property-ken

## 6. API konvenciók

- Verzionálás: URL prefix (`/api/v1/`)
- Hibakezelés: RFC 7807 Problem Details formátum
- Soft delete: `IsDeleted = true`, GET végpontok alapból kiszűrik
- Lapozás: `page` (default 1) + `pageSize` (default 20, max 100)
- Feature kikapcsolt: `501 Not Implemented` + `{ "feature": "{Name}" }`
- Minden endpoint XML kommenttel + `[ProducesResponseType]` attribútumokkal
- Publikus, auth nélküli végpontok: `/health`, `/api/v1/config/features`, `/api/v1/auth/login`

## 7. Git workflow és commit konvenciók

### Branch-ek
- Branch-ek mindig `test`-ről ágaznak el, sosem `main`-ről
- `main` és `test` ágra közvetlen push tiltott
- Naming: `feature/{domain}-{leírás}`, `fix/{jegy}-{leírás}`, `docs/{téma}`, `refactor/{hatókör}-{leírás}`, `chore/{leírás}`

### PR csoportosítás – kohézió-alapú

A PR egysége nem az issue, hanem az **összetartozó, koherens munka**:

| Fázis | PR csoportosítás | Branch példa | Tartalom |
| ----- | ---------------- | ------------ | -------- |
| Phase 0 (Foundation) | Teljes fázis = 1 PR | `chore/phase0-foundation` | Minden infra/scaffolding issue |
| Phase 1+ (Features) | Domain-önként 1 PR | `feature/auth-local-provider` | Az adott domain összes issue-ja (interfész + provider + controller + tesztek + doksik) |
| Hotfix | Issue-nként 1 PR | `fix/42-null-owner-team` | Egyetlen hibajavítás |

**Szabályok:**
- Egy branch **több issue-t** tartalmazhat, ha azok ugyanahhoz a domainhez/fázishoz tartoznak
- Minden érintett issue commitjának láblécében legyen `Closes #<szám>`
- Domain-szintű branch-ben az egyes issue-k külön commitok legyenek
- `test` → `main` PR fázis végén (production release)

### Conventional Commits
```
<típus>(<hatókör>): <leírás>
```

**Típusok:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`, `perf`

**Hatókörök:** `auth`, `catalog`, `search`, `docs`, `rbac`, `infra`, `api`, `frontend`, `admin`, `audit`

**Szabályok:**
- Leírás imperatívban, kisbetűvel kezdődik, nincs pont a végén
- Maximum 72 karakter
- Issue referencia: `Closes #42` a lábléc szekcióban

### PR folyamat
- PR cím = fő commit üzenet formátum (ha több domain érintett, a legfontosabb)
- PR mindig `test`-re megy, nem `main`-re
- Min. 1 code review szükséges
- CI zöld legyen merge előtt
- `test` → `main` PR fázis végén, squash merge tilos (megőrzi a history-t)

## 8. Definition of Done (DoD)

Minden feature PR-nál ellenőrizd:

### Kód
- [ ] Unit tesztek (min. 80% lefedettség az új kódban)
- [ ] Integrációs teszt (happy path + 403 + 404)
- [ ] EF Core migráció + PostgreSQL DDL szkript (ha DB változott)
- [ ] Feature flag implementálva (`Enabled: true/false`)
- [ ] NullProvider implementálva (ha pluggable komponens)

### Fejlesztői dokumentáció (`docs/developer/{domain}/`)
- [ ] `overview.md` – architektúra, főbb osztályok
- [ ] `configuration.md` – összes config kulcs
- [ ] `providers.md` – providerek leírása, csere menete
- [ ] `troubleshooting.md` – ismert hibák és megoldások

### Felhasználói dokumentáció (`docs/user/`)
- [ ] Érintett user doc létezik vagy frissítve
- [ ] Nem tartalmaz technikai részleteket

### Ops dokumentáció (`docs/ops/`)
- [ ] Ha konfig/secret változott, frissítve
- [ ] Tartalmaz: env variable-ok, K8s Secret-ek neve, rollback

### OpenAPI
- [ ] XML kommentek minden új/módosított endpointon
- [ ] `[ProducesResponseType]` attribútumok megvannak

### Justfile karbantartás
- [ ] Ha a PR új gyakori parancsot vezet be (lint, migráció, stb.), a `justfile` frissítése a PR része
- [ ] Pre-commit és CI a justfile receptjeit hívja ahol lehetséges (egyetlen forrás)

## 9. Biztonsági szabályok

- Jelszó-hash: PBKDF2 (`PasswordHasher<T>`), per-user salt; plain jelszó soha nem tárolódik/logolódik
- Session: `HttpOnly` + `Secure` + `SameSite=Lax` cookie
- Secretek (jelszavak, keytab-ok, service account adatok) soha nem kerülnek logba
- URL validáció: csak `http`/`https` séma megengedett
- Jogosultság-kiértékelési sorrend: Auth → Feature flag → IsActive → Szerepkör → Erőforrás-szintű
- Jogosultság-szűrés query szinten is (nem csak HTTP szinten) – pl. developer doksi EF query-ben is szűrt

## 10. MCP szerver szabályok (spec 12.4)

- MCP tool-ok **nem** párhuzamos implementáció – a meglévő domain-szolgáltatásra hívnak
- Minden tool-hívás auditálódik (`IAuditProvider`, `Action = "McpToolCall"`)
- RBAC query-szinten érvényesül, az AI soha nem kap olyan tartalmat, amit a token szerepköre nem láthat
- v1.0-ban csak olvasási tool-ok (`ExposeWriteTools = false`)
- `Features:Mcp:Enabled = false` esetén az endpoint nincs map-elve
- Tool dokumentáció: `docs/integrations/mcp-server.md`

## 11. Issue lezárási szabály

Amikor egy GitHub issue implementációja elkészül:

1. **Commit** – a commit üzenet láblécében legyen `Closes #<szám>`
2. **GitHub komment** – az issue-ra írd meg az implementációs összefoglalót:
   - Mely checklist pontok teljesültek (kipipálva)
   - Létrehozott/módosított fájlok rövid felsorolása
   - Releváns technikai döntések, ha voltak
3. **Sorrend** – a komment a push előtt vagy közvetlenül utána történjen meg, ne maradjon el

Ez minden issue-ra vonatkozik, típustól függetlenül (feat, fix, chore, docs).

## 12. Dokumentáció fájl-struktúra

```
docs/
├── developer/{domain}/     # overview.md, configuration.md, providers.md, troubleshooting.md
├── user/                   # Felhasználói dokumentáció (technikai részletek nélkül)
├── ops/                    # Üzemeltetési: env változók, K8s Secrets, rollback
├── integrations/           # MCP szerver konfiguráció, AI-kliens setup
└── adr/                    # ADR-{szám}-{name}.md (Kontextus, Döntés, Következmények, Elutasított alternatívák)
```
