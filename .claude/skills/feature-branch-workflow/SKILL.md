---
name: feature-branch-workflow
description: >
  A SoftwareInventory projekt git-workflow és branch-konvenciók betartásához.
  Triggerelj, ha:
  - A felhasználó új feature-t kezd fejleszteni (branch név kell)
  - Commit üzenet szövegezése szükséges
  - PR leírást kell összeállítani
  - DoD (Definition of Done) ellenőrzés szükséges merge előtt
  - Meglévő commit üzenet formátumát kell ellenőrizni vagy javítani
  - Branch konvenció kérdése merül fel
---

# Feature Branch Workflow Skill

Ez a skill a SoftwareInventory projekt branch- és commit-konvencióit tartatja be,
és a PR/merge folyamat minden lépésénél segítséget nyújt.

---

## Branch naming konvenciók

| Típus              | Minta                             | Példa                              |
| ------------------ | --------------------------------- | ---------------------------------- |
| Fázis scaffolding  | `chore/phase{N}-{leírás}`        | `chore/phase0-foundation`          |
| Új feature (domain)| `feature/[domain]-[rövid-leírás]` | `feature/auth-local-provider`      |
| Hibajavítás        | `fix/[jegy-szám]-[rövid-leírás]`  | `fix/42-null-owner-team`           |
| Dokumentáció       | `docs/[téma]`                     | `docs/kerberos-setup-guide`        |
| Refaktorálás       | `refactor/[hatókör]-[leírás]`     | `refactor/rbac-group-mapper`       |
| CI/infra           | `chore/[leírás]`                  | `chore/add-ci-pipeline`            |

**Szabályok:**

- Branch neve csupa kisbetű, kötőjellel elválasztva (kebab-case)
- Minden branch `test`-ről ágazik el, sosem `main`-ről
- A branch neve tükrözze a tartalmat, ne legyen általános (`fix/misc`, `feature/stuff` tiltott)

**Branch létrehozása:**

```bash
git checkout test
git pull origin test
git checkout -b feature/search-postgres-fts
```

## PR csoportosítás – kohézió-alapú

A PR egysége nem az issue, hanem az **összetartozó, koherens munka**:

| Fázis | PR csoportosítás | Tartalom |
| ----- | ---------------- | -------- |
| Phase 0 (Foundation) | Teljes fázis = 1 PR | Minden infra/scaffolding issue együtt |
| Phase 1+ (Features) | Domain-önként 1 PR | Egy domain összes issue-ja (interfész + provider + controller + tesztek + doksik) |
| Hotfix | Issue-nként 1 PR | Egyetlen hibajavítás |

**Szabályok:**
- Egy branch **több issue-t** tartalmazhat, ha azok ugyanahhoz a domainhez/fázishoz tartoznak
- Domain-szintű branch-ben az egyes issue-k **külön commitok** legyenek (issue-nként `Closes #X` a commitban)
- `test` → `main` PR fázis végén (production release)

---

## Commit üzenet konvenciók (Conventional Commits)

### Formátum

```
<típus>(<hatókör>): <leírás>

[opcionális törzs – ha bővebb magyarázat szükséges]

[opcionális lábléc – breaking change vagy issue referencia]
```

### Engedélyezett típusok és hatókörök

**Típusok:**

| Típus      | Mikor                                                   |
| ---------- | ------------------------------------------------------- |
| `feat`     | Új funkció, endpoint, komponens                         |
| `fix`      | Hibajavítás                                             |
| `docs`     | Kizárólag dokumentáció-változás                         |
| `refactor` | Kód átszervezés, funkcionalitás-változás nélkül         |
| `test`     | Tesztek hozzáadása vagy javítása                        |
| `chore`    | Build, CI, függőség, infra – nem érint production kódot |
| `ci`       | CI/CD pipeline változás                                 |
| `perf`     | Teljesítmény-javítás                                    |

**Hatókörök:**

| Hatókör    | Leírás                                                         |
| ---------- | -------------------------------------------------------------- |
| `auth`     | Authentikáció, provider, Kerberos, LDAP                        |
| `catalog`  | Alkalmazás-katalógus CRUD                                      |
| `search`   | Keresési funkció és providerek                                 |
| `docs`     | Dokumentáció-kezelő funkció (nem maguk a md fájlok!)           |
| `rbac`     | Szerepkörök, jogosultságok, GroupRoleMapping                   |
| `infra`    | Docker, K8s, CI konfiguráció                                   |
| `api`      | Általános API szintű változás (pl. versioning, error handling) |
| `frontend` | React frontend                                                 |
| `admin`    | Admin panel                                                    |
| `audit`    | Audit log funkció                                              |

### Szabályok

- A leírás imperatívban, kisbetűvel kezdődik, nincs pont a végén
- A leírás maximum 72 karakter
- Ha breaking change van: `BREAKING CHANGE:` a lábléc szekcióban
- Jegy referencia: `Closes #42` a lábléc szekcióban

### Helyes commit üzenetek – példák

```
feat(auth): implement Kerberos SPNEGO provider with keytab validation
feat(auth): add LDAP group sync with 15-minute memory cache
fix(catalog): null reference exception when OwnerTeam is empty string
fix(rbac): group-role mapping not applied after cache expiry
docs(search): add PostgreSQL FTS configuration guide
docs(auth): add kerberos-setup.md with SPN and keytab steps
refactor(rbac): extract GroupRoleMapper to dedicated service class
test(catalog): add integration tests for soft-delete behavior
chore(infra): add libgssapi-krb5-2 to Dockerfile base image
feat(search): add PostgresFts provider with GIN index and ts_rank_cd
feat(docs-module): add documentation version history with 90-day retention
feat(admin): add GroupRoleMapping CRUD endpoints [Admin]
```

### Hibás commit üzenetek – ezeket javítani kell

```
❌ Fixed stuff
❌ WIP
❌ feat: various changes
❌ Update application controller
❌ feat(auth): Implement Kerberos.   (pont a végén, nagybetűvel kezdődik)
❌ fix: null ref (túl általános, nem derül ki mi nulláz)
```

---

## Pull Request folyamat

### PR létrehozása `test`-re

**PR cím formátuma:** Conventional Commit stílus, a PR fő tartalmát tükrözi:

```
feat(auth): implement local auth provider with PBKDF2 password hashing
chore(infra): Phase 0 foundation – solution, Docker, CI
```

Ha a PR több domaint érint, a legfontosabb változás legyen a címben.

**PR leírás sablon:**

```markdown
## Összefoglalás

<!-- Mit csinál ez a PR? 2-3 mondatban. -->

## Változások

<!-- Konkrét lista, mi változott. -->

- [ ] IAuthProvider interfész létrehozva
- [ ] KerberosAuthProvider implementálva
- [ ] LdapGroupProvider 15 perces cache-szel
- [ ] DI regisztráció és feature flag

## Dokumentáció

- [ ] `docs/developer/auth/overview.md` – létrehozva
- [ ] `docs/developer/auth/kerberos-setup.md` – létrehozva
- [ ] `docs/user/getting-started.md` – frissítve

## Tesztelés

<!-- Hogyan tesztelted? Mi az, amit manuálisan ellenőrizni kell? -->

## Definition of Done ellenőrzés

<!-- A merge előtt minden sornak teljesülnie kell. -->

### Kód

- [ ] Unit tesztek írva (min. 80% az új kódban)
- [ ] Integrációs teszt: happy path + 401 + 403
- [ ] EF Core migráció + PostgreSQL DDL szkript (ha DB változott)
- [ ] Feature flag (`Enabled`) + NullProvider implementálva

### Fejlesztői dokumentáció (`docs/developer/`)

- [ ] overview.md: architektúra, fő osztályok
- [ ] configuration.md: összes config kulcs leírva
- [ ] providers.md: elérhető providerek, csere menete
- [ ] troubleshooting.md: ismert hibák és megoldások

### Felhasználói dokumentáció (`docs/user/`)

- [ ] Érintett user doc létezik vagy frissítve van
- [ ] Nem tartalmaz technikai részleteket

### Ops dokumentáció (`docs/ops/`)

- [ ] Ha konfig/secret változott, frissítve van
- [ ] Tartalmaz: env változók, K8s Secret-ek neve, rollback eljárás

### OpenAPI

- [ ] Minden új/módosított endpoint XML kommenttel ellátva
- [ ] `[ProducesResponseType]` attribútumok megvannak
```

---

## DoD ellenőrzési workflow (merge előtt)

Ha a felhasználó kéri a DoD ellenőrzést, az alábbi sorrendben nézd át:

### 1. Kód ellenőrzés

```bash
# Build és tesztek
dotnet build
dotnet test --collect:"XPlat Code Coverage"

# Migráció meglétének ellenőrzése
ls sql/migrations/  # ott van-e a legutóbbi migráció DDL-je?

# Feature flag: megvan-e a NullProvider?
grep -r "NullProvider\|IsAvailable => false" src/
```

### 2. Dokumentáció-ellenőrzés

Az érintett domain-hez megvan-e a négy developer doc:

- `docs/developer/{domain}/overview.md`
- `docs/developer/{domain}/configuration.md`
- `docs/developer/{domain}/providers.md`
- `docs/developer/{domain}/troubleshooting.md`

User doc frissítve van-e, ha a feature felhasználó-érintő?

### 3. OpenAPI ellenőrzés

```bash
# Minden controllerben megvan-e a dokumentáció?
grep -r "ProducesResponseType\|<summary>" src/AppInventory.Api/Controllers/
```

---

## Merge `test` → `main` (production release)

1. Staging környezeten manuális elfogadási teszt elvégzése
2. PR nyitása `test` → `main`
3. PR leírásában: `## Release notes` szekció a felhasználók számára érthető változásleírással
4. 1 review + CI zöld
5. Squash merge tilos `test` → `main` irányban (megőrzi a history-t)
6. Tag létrehozása merge után: `v{major}.{minor}.{patch}`

```bash
git tag -a v1.2.0 -m "Release v1.2.0: PostgreSQL FTS search"
git push origin v1.2.0
```
