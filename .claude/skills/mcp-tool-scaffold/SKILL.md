---
name: mcp-tool-scaffold
description: >
  A SoftwareInventory MCP szerveréhez (specifikáció 12.4) új MCP tool vagy toolset
  scaffoldingjához. Triggerelj, ha:
  - A felhasználó új MCP tool-t kér, amit AI-asszisztens hívhat (pl. "expose application
    dependencies as an MCP tool", "add a get_application_owners MCP tool")
  - Új MCP toolset osztály kell (pl. `CatalogMcpToolset`, `DocumentationMcpToolset`)
  - Magát az MCP szervert kell beállítani (Streamable HTTP, feature flag, regisztráció)
  - MCP tool dokumentáció vagy kliens-konfiguráció (`docs/integrations/mcp-server.md`) kell
  Soha ne generálj MCP tool-t RBAC + feature-flag + audit kötés és dokumentáció nélkül,
  és soha ne implementálj párhuzamos üzleti logikát – a tool a meglévő domain-szolgáltatásra hívjon!
---

# MCP Tool Scaffold Skill

Ez a skill a SoftwareInventory **publikált** MCP szerveréhez (specifikáció 12.4) generál
új tool-t vagy toolset-et a projekt konvencióival. A cél: a rendszer AI-asszisztensekhez
adható eszközként, miközben minden hívás a meglévő RBAC, feature-flag és audit rétegen megy át.

## Kötelező alapelvek (a spec 12.4 alapján)

1. **Nincs párhuzamos logika** – a tool a meglévő domain-szolgáltatást hívja
   (`ApplicationService`, `ISearchProvider`, `IDocumentStore`), nem implementál újra semmit.
2. **RBAC mindig** – az adat-szűrés a query-szintű RBAC-ra (spec 8.5) támaszkodik; az AI soha
   nem kap olyan tartalmat, amit a tokenhez rendelt szerepkör nem láthat.
3. **Feature-flag-tudat** – ha `Features:Mcp:Enabled = false`, az endpoint nincs map-elve.
4. **Olvasás alapból** – v1.0-ban csak read-only tool. Írási tool csak akkor, ha
   `Features:Mcp:ExposeWriteTools = true`, és csak `ApplicationOwner`/`Developer`/`Admin` tokennel.
5. **Audit** – minden tool-hívás `IAuditProvider`-rel naplózódik (`Action = "McpToolCall"`).
6. **Dokumentáció** – a tool felvétele a `docs/integrations/mcp-server.md`-ben is megjelenik.

---

## Workflow

### 1. lépés – Azonosítás

Azonosítsd:

- A tool nevét snake_case-ben (MCP konvenció), pl. `get_application_dependencies`
- Mely meglévő domain-szolgáltatás adja az adatot (ne hozz létre újat indok nélkül)
- A toolset-osztályt, amibe tartozik (`CatalogMcpToolset`, `DocumentationMcpToolset`, vagy új)
- Olvasó vagy író-e (alap: olvasó)
- A minimálisan szükséges szerepkört (alap: `ReadOnly`)

### 2. lépés – Toolset osztály (ha új kell)

Hely: `src/AppInventory.Infrastructure/Mcp/{Terület}McpToolset.cs`

```csharp
// src/AppInventory.Infrastructure/Mcp/{Terület}McpToolset.cs
using ModelContextProtocol.Server;
using System.ComponentModel;

/// <summary>
/// {Terület} MCP tool-ok. A SoftwareInventory MCP szerverén publikálva (spec 12.4).
/// Minden tool a meglévő domain-szolgáltatásra hív; az RBAC query-szinten érvényesül.
/// </summary>
[McpServerToolType]
public sealed class {Terület}McpToolset    // jelölő szerep: IMcpToolset
{
    // A tool-metódusok a függőségeiket paraméterként, DI-n keresztül kapják.
}
```

### 3. lépés – Tool metódus generálása

A toolset osztályon belül:

```csharp
[McpServerTool, Description("{Egyértelmű, AI-nak szóló leírás arról, mit ad vissza a tool.}")]
public async Task<{ResultDto}> {ToolNeve}(
    {DomainService} service,                       // DI-n keresztül injektált meglévő szolgáltatás
    IAuditProvider audit,                          // audit naplózáshoz
    [Description("{Paraméter leírása az AI-nak}")] {ParamType} {param},
    CancellationToken ct = default)
{
    // 1) Audit – minden tool-hívás naplózódik
    await audit.LogAsync("McpToolCall", resourceType: "{tool_neve}", resourceId: "{param}", ct);

    // 2) A meglévő domain-szolgáltatás hívása – az RBAC ott, query-szinten érvényesül (spec 8.5).
    //    A tool soha nem kerüli meg a jogosultság-szűrést.
    return await service.{LétezőMetódus}({param}, ct);
}
```

> **Írási tool esetén** (csak ha `ExposeWriteTools = true`): a metódus elején ellenőrizd a
> `Features:Mcp:ExposeWriteTools` flaget és a hívó szerepkörét (`ApplicationOwner`/`Developer`/`Admin`),
> egyébként dobj engedélyezési hibát. A mutáció ugyanazon a domain-szolgáltatáson keresztül történjen,
> mint a REST controller.

### 4. lépés – Regisztráció (ha új toolset)

A `Features:Mcp` feature-flaghez kötött extension-ben (spec 12.4.2):

```csharp
// src/AppInventory.Api/Extensions/McpServiceExtensions.cs
public static IServiceCollection AddMcpServerFeature(
    this IServiceCollection services, IConfiguration configuration)
{
    var section = configuration.GetSection("Features:Mcp");
    if (!section.GetValue<bool>("Enabled"))
        return services;   // nincs MCP szerver – endpoint sem lesz map-elve

    services.AddMcpServer()
        .WithHttpTransport(o => o.Stateless = section.GetValue("Stateless", true))
        .WithTools<CatalogMcpToolset>()
        .WithTools<DocumentationMcpToolset>()
        .WithTools<{Terület}McpToolset>();   // ← új toolset ide

    return services;
}
```

**Program.cs (kötelező, ha még nincs):**

```csharp
builder.Services.AddMcpServerFeature(builder.Configuration);
// ...
if (builder.Configuration.GetValue<bool>("Features:Mcp:Enabled"))
    app.MapMcp("/mcp");
```

### 5. lépés – Konfigurációs séma ellenőrzése

Az `appsettings.json`-ban a `Features:Mcp` szekciónak léteznie kell (spec 5.1):

```json
"Features": {
  "Mcp": {
    "Enabled": false,
    "Transport": "StreamableHttp",
    "Stateless": true,
    "RequireAuthentication": true,
    "DefaultRole": "ReadOnly",
    "ExposeWriteTools": false
  }
}
```

### 6. lépés – Feature API frissítése

A `/api/v1/config/features` válasznak tartalmaznia kell az `mcp` kulcsot:

```csharp
"mcp": new { enabled = featureManager.IsEnabled("Mcp") }
```

### 7. lépés – Dokumentáció

**`docs/integrations/mcp-server.md`** – az új tool felvétele a publikált tool-ok táblázatába:

```markdown
| Tool             | Leírás                          | Szükséges szerepkör | Mögöttes szolgáltatás |
| ---------------- | ------------------------------- | ------------------- | --------------------- |
| `{tool_neve}`    | {rövid leírás}                  | {ReadOnly/...}      | `{DomainService}`     |
```

Ha még nem létezik, hozd létre a fájlt a következő vázzal:

```markdown
# SoftwareInventory MCP szerver – telepítés és AI-kliens konfiguráció

## Áttekintés
A SoftwareInventory MCP szerverként publikálja a katalógust, dokumentációt és keresést
(spec 12.4). Streamable HTTP transport, `/mcp` útvonal.

## Bekapcsolás
`Features:Mcp:Enabled = true` és service token kiállítása (`DefaultRole`).

## Publikált tool-ok
(táblázat – ld. fent)

## Hozzáadás AI-klienshez (példa)
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

## Biztonság
Auth kötelező, RBAC query-szinten, minden hívás auditálva, írási tool-ok v1.0-ban tiltva.
```

---

## Ellenőrző lista generálás után

- [ ] Tool-metódus `[McpServerTool, Description(...)]` attribútummal, beszédes AI-leírással
- [ ] A tool **meglévő** domain-szolgáltatást hív (nincs párhuzamos logika)
- [ ] RBAC a query-szinten érvényesül; nem-publikus/developer tartalom token-szerepkör szerint szűrt
- [ ] Audit naplózás (`IAuditProvider`, `Action = "McpToolCall"`)
- [ ] Olvasó tool (vagy író esetén `ExposeWriteTools` + szerepkör-ellenőrzés)
- [ ] Toolset regisztrálva az `AddMcpServerFeature` extension-ben (feature-flag mögött)
- [ ] `Features:Mcp` szekció megvan az `appsettings.json`-ban
- [ ] `/api/v1/config/features` tartalmazza az `mcp` kulcsot
- [ ] `docs/integrations/mcp-server.md` frissítve az új tool-lal

---

## Tervezési emlékeztető

- A tool-ok **nem** új REST-réteg: ugyanazokat a service-eket használják, mint a controllerek.
- Stateless HTTP mód ajánlott (horizontális skálázás, K8s – spec P5).
- Az identitás v1.0-ban service token (`DefaultRole`), v2.0-ban OAuth 2.1 (felhasználó nevében).
- Kétség esetén olvasó tool-t generálj; írási képességet csak explicit kérésre.
