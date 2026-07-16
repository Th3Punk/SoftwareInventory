# SoftwareInventory MCP szerver – Telepítés és AI-kliens konfiguráció

## Áttekintés

A SoftwareInventory MCP (Model Context Protocol) szerverként publikálja az alkalmazás-katalógust,
a dokumentációkat és a keresést (spec 12.4). AI-asszisztensek így természetes nyelven kérdezhetik
le az inventory tartalmát a REST API közvetlen ismerete nélkül.

**Transport:** Streamable HTTP (stateless), `/mcp` útvonal  
**Hitelesítés:** Bearer service token (`Authorization: Bearer <token>`)  
**RBAC:** query-szintű szűrés — az AI soha nem kap olyat, amit a token szerepköre nem láthat  
**Audit:** minden tool-hívás naplózódik (`IAuditProvider`, `Action = "McpToolCall"`)

---

## Bekapcsolás

1. Állítsd `Features:Mcp:Enabled = true` értékre az `appsettings.json`-ban  
   (vagy K8s ConfigMap-ben: `Features__Mcp__Enabled=true`).
2. Generálj service token(eket) és add hozzá a konfigurációhoz:

   ```
   Features__Mcp__ServiceTokens__0=<opaque-token-1>
   Features__Mcp__ServiceTokens__1=<opaque-token-2>
   ```

   A tokeneket **K8s Secret**-ben tárold, soha ne push-old a forráskódba.
3. Indítsd újra az alkalmazást — a `/mcp` endpoint aktív lesz.

### Konfigurációs kulcsok

| Kulcs | Alapértelmezés | Leírás |
|---|---|---|
| `Features:Mcp:Enabled` | `false` | MCP szerver be/ki |
| `Features:Mcp:Stateless` | `true` | Stateless HTTP mód (ajánlott K8s-ben) |
| `Features:Mcp:DefaultRole` | `ReadOnly` | Service token alapértelmezett szerepköre |
| `Features:Mcp:ExposeWriteTools` | `false` | Írási tool-ok engedélyezése (v2.0) |
| `Features:Mcp:ServiceTokens` | `[]` | Érvényes bearer tokenek listája (K8s Secret-ből) |

---

## Publikált tool-ok (v1.0 – csak olvasás)

| Tool | Leírás | Szükséges szerepkör | Mögöttes réteg |
|---|---|---|---|
| `search_applications` | Teljes szöveges keresés az alkalmazások között | ReadOnly+ | `ISearchProvider` |
| `list_applications` | Szűrhető, lapozható alkalmazás-lista | ReadOnly+ | `AppInventoryDbContext` |
| `get_application` | Részletes alkalmazás-nézet (env URL-ek, kapcsolattartók) | ReadOnly+ | `AppInventoryDbContext` |
| `get_application_environments` | Nyilvános deployment URL-ek egy alkalmazáshoz | ReadOnly+ | `AppInventoryDbContext` |
| `list_documentation` | Felhasználói dokumentációk listája egy alkalmazáshoz | ReadOnly+ | `AppInventoryDbContext` |
| `get_documentation` | Egy dokumentum teljes Markdown tartalma | ReadOnly+ | `AppInventoryDbContext` |

**RBAC-korlátozások ReadOnly tokennél:**

- Csak **nyilvános** (`IsPublic = true`) deployment URL-ek láthatók
- Csak **User típusú** dokumentációk elérhetők (Developer és Operations doksi nem)
- Az alkalmazás-lista és -részletek teljes tartalmukban elérhetők

---

## Hozzáadás AI-klienshez

### Claude Code / `.mcp.json`

```json
{
  "mcpServers": {
    "software-inventory": {
      "type": "http",
      "url": "https://appinventory.cegdomain.local/mcp",
      "headers": {
        "Authorization": "Bearer ${SOFTWARE_INVENTORY_MCP_TOKEN}"
      }
    }
  }
}
```

Állítsd be a `SOFTWARE_INVENTORY_MCP_TOKEN` környezeti változót a kiosztott service tokenre.

### Claude Desktop (`claude_desktop_config.json`)

```json
{
  "mcpServers": {
    "software-inventory": {
      "type": "http",
      "url": "https://appinventory.cegdomain.local/mcp",
      "headers": {
        "Authorization": "Bearer <your-service-token>"
      }
    }
  }
}
```

---

## Biztonság

- Az MCP endpoint **azonos** auth + rate-limiting pipeline mögött ül, mint a REST API
- A Bearer token hossz-alapú timing-safe összehasonlítással (`CryptographicOperations.FixedTimeEquals`) kerül ellenőrzésre
- Írási tool-ok v1.0-ban tiltva (`ExposeWriteTools = false`); feltételük v2.0-ban: `ApplicationOwner`/`Developer`/`Admin` token + explicit engedélyezés
- Nem-nyilvános URL-ek és developer/ops doksi nem érhető el ReadOnly tokennel
- Minden tool-hívás auditálódik az audit log táblában

---

## Új tool hozzáadása

Új tool felvételéhez használd az `mcp-tool-scaffold` skillt:

```
/mcp-tool-scaffold
```

A skill végigvezet az interfész, implementáció, regisztráció és dokumentáció lépésein,
biztosítva hogy az új tool is a projekt RBAC, audit és feature-flag konvencióit követi.
