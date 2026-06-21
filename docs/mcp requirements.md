# SoftwareInventory – Szükséges MCP szerverek

**Dokumentum dátuma:** 2026-06-18

Az alábbi MCP szerverek a SoftwareInventory projekt fejlesztési workflow-jához szükségesek.
A lista tartalmazza a szerver szerepét, a fejlesztési fázist amelytől szükséges, és az integrálás módját.

> **Irány megkülönböztetése:** Ez a dokumentum a fejlesztés során **fogyasztott** (a csapat/AI által használt)
> MCP szervereket sorolja. Ettől külön kérdés, hogy a SoftwareInventory **maga is publikál** egy MCP szervert,
> amellyel AI-asszisztensekhez adható eszközként (katalógus, dokumentáció, keresés lekérdezése). Ennek a
> **publikált** MCP szervernek a tervezése a `specification.md` 12.4 szakaszában található; új tool felvételét az
> `mcp-tool-scaffold` skill segíti.

---

## Kötelező (1. fázistól)

### 1. Filesystem MCP

**Szerepe:** Projektfájlok, specifikáció-dokumentumok, skill fájlok közvetlen olvasása és írása.

**Fejlesztési workflow-ban:**

- A spec (`appinventory-spec.md`) tartalmának ellenőrzése implementáció előtt
- Dokumentáció fájlok (`docs/developer/`, `docs/user/`, `docs/ops/`) közvetlen írása és frissítése
- EF Core migráció fájlok olvasása a DDL generáláshoz
- `appsettings.json` és Docker/K8s konfig fájlok módosítása

**Konfiguráció:** Az engedélyezett útvonalak a projekt gyökérkönyvtárára korlátozzák az elérést.

---

### 2. Git / Gitea MCP

**Szerepe:** Branch-kezelés, commit-history megtekintés, PR-műveletek.

**Fejlesztési workflow-ban:**

- Feature branch létrehozása és checkout (`feature/auth-kerberos-provider`)
- Commit history ellenőrzése az ADR-ek és döntések nyomon követéséhez
- Branch státusz, open PR-ok áttekintése
- Merge státusz ellenőrzése a DoD folyamat részeként

**Konfiguráció:** A vállalati Gitea instance URL-jére mutat; olvasási és írási jogosultság szükséges.

---

### 3. PostgreSQL MCP

**Szerepe:** Az adatbázis-séma élő inspekciója, migráció-validálás, fejlesztés közbeni lekérdezések.

**Fejlesztési workflow-ban:**

- Az aktuális séma megtekintése migráció írása előtt (`\d tablename`)
- EF Core migráció lefuttatásának ellenőrzése (`__EFMigrationsHistory`)
- FTS index-ek ellenőrzése (`pg_indexes` view)
- Fejlesztési adatok lekérdezése és validálása implementáció közben
- Teljesítmény-ellenőrzés: lekérdezési tervek (`EXPLAIN ANALYZE`)

**Konfiguráció:**

```
Host: localhost (fejlesztői Docker Compose)
Port: 5432
Database: appinventory_dev
User: appinventory (read+write)
```

**Megjegyzés:** Production adatbázisra csak olvasási jogosultsággal csatlakozzon.

---

## Ajánlott (fejlesztői hatékonysághoz)

### 4. Context7 MCP

**Szerepe:** .NET, ASP.NET Core, EF Core, Npgsql dokumentáció offline lookup-ja.

**Fejlesztési workflow-ban:**

- ASP.NET Core Negotiate middleware konfigurációs részletek
- EF Core 9 JSON oszlop és FTS lehetőségek (Npgsql-specifikus)
- Microsoft.FeatureManagement API referencia
- React 19 hook API, TypeScript type utility-k

**Fontos korlátozás:** Ha a fejlesztői környezet air-gapped (nincs internet-hozzáférés),
a Context7 offline RAG snapshot-ot igényel. Ld. a vállalati on-premise AI platform
dokumentációját a snapshot frissítési eljárásért.

---

### 5. MSSQL MCP (opcionális alternatíva a PostgreSQL MCP-hez)

**Szerepe:** Ha a csapat MS SQL-re vált vissza (ld. ADR-002), a PostgreSQL MCP helyett ez kerül használatba.

**Fejlesztési workflow-ban:** azonos mint a PostgreSQL MCP, T-SQL szintaxissal.

**Konfiguráció:** Ld. a meglévő vállalati MSSQL MCP konfigurációt.

---

## Jövőbeli bővítmények (2. fázistól)

### 6. Slack / Microsoft Teams MCP

**Szerepe:** Deploy értesítések, code review kérések, incident riasztások automatizálása.

**Fázis:** A `NotificationProvider` implementálásakor válik relevánssá (Fázis 4).

---

### 7. Kubernetes MCP (ha elérhető)

**Szerepe:** K8s deployment státusz, pod logok, secret kezelés ellenőrzése.

**Fejlesztési workflow-ban:**

- Deploy státusz ellenőrzése (`kubectl get pods`)
- Pod logok megtekintése (Kerberos auth hibakereséshez különösen hasznos)
- ConfigMap és Secret tartalmak ellenőrzése (értékek nélkül – csak kulcs-nevek)

---

## Skill telepítési útmutató

A projekthez készült skill-ek (`pluggable-feature-scaffold`, `feature-branch-workflow`,
valamint a meglévő `ef-migration-sql` skill PostgreSQL adaptációja) a projekt
`.claude/skills/` mappájába kerülnek, hogy verziókezelés alatt legyenek:

```
{projekt-gyökér}/
└── .claude/
    └── skills/
        ├── pluggable-feature-scaffold/
        │   └── SKILL.md
        ├── feature-branch-workflow/
        │   └── SKILL.md
        └── ef-migration-postgresql/
            └── SKILL.md      ← a PostgreSQL DDL generáló skill (létrehozandó)
```

**Megjegyzés az `ef-migration-postgresql` skillről:**
A meglévő `ef-migration-sql` skill MS SQL T-SQL szintaxist generál. Mivel ez a projekt
PostgreSQL-t használ, egy `ef-migration-postgresql` skill szükséges, amely PostgreSQL DDL-t
(`CREATE TABLE`, `ALTER TABLE` stb. PostgreSQL szintaxissal) generál. Ez a skill az
`ef-migration-sql` skill mintájára készítendő el a projekt korai fázisában.

---

## Összefoglalás

| MCP szerver | Prioritás  | Fázis | Fő használat                       |
| ----------- | ---------- | ----- | ---------------------------------- |
| Filesystem  | Kötelező   | 1.    | Spec, docs, config fájlok          |
| Git / Gitea | Kötelező   | 1.    | Branch, commit, PR                 |
| PostgreSQL  | Kötelező   | 1.    | Séma inspekció, migráció-validálás |
| Context7    | Ajánlott   | 1.    | .NET/EF Core docs lookup           |
| MSSQL       | Opcionális | —     | Ha MS SQL-re váltanak              |
| Slack/Teams | Jövőbeli   | 4.    | Értesítések                        |
| Kubernetes  | Jövőbeli   | 3.    | Deploy, logok                      |
