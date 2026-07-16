# ADR-004 – SoftwareInventory MCP szerver publikálása

**Dátum:** 2026-07-16  
**Státusz:** Elfogadva  
**Döntéshozók:** Fejlesztői csapat

---

## Kontextus

A SoftwareInventory tartalma (alkalmazás-katalógus, dokumentációk, keresés) egyre inkább
szükséges AI-asszisztensek számára is, amelyek a fejlesztők és üzemeltetők munkaeszközeivé
válnak. A hagyományos REST API megköveteli, hogy az AI-kliens ismerje az endpoint-struktúrát
és a lekérdezési paramétereket. A Model Context Protocol (MCP) standardizált, AI-natív
interfészt biztosít, amely lehetővé teszi, hogy a szerver saját maga írja le képességeit
tool-definíciókon keresztül.

A döntési kontextus:

- A csapat Claude Code-ot, Claude Desktop-ot és vállalati AI platformot használ
- Az inventory tartalom (ki mit fejleszt, hol fut, hogyan dokumentált) az AI-asszisztens
  szempontjából kritikus kontextuális adat
- A jelenlegi helyzet: az AI vagy REST kéréseket generál (fragilis) vagy nincs hozzáférése az adatokhoz

---

## Döntés

A SoftwareInventory MCP szerverként publikálja az alkalmazás-katalógust, dokumentációkat és
keresést a `ModelContextProtocol.AspNetCore` SDK segítségével, Streamable HTTP transport-on,
a `/mcp` útvonalon.

**Kulcsdöntések:**

1. **Nem párhuzamos implementáció** – Az MCP tool-ok a meglévő DbContext lekérdezéseket
   és `ISearchProvider`-t hívják, nem implementálnak új üzleti logikát.

2. **RBAC query-szinten** – Az adatszűrés ugyanott történik, ahol a REST API-ban: EF Core
   queryekben, nem HTTP rétegben. Az AI soha nem láthat olyat, amit a token nem engedélyez.

3. **Service token auth v1.0-ban** – Bearer token a `Features:Mcp:ServiceTokens` listából,
   `DefaultRole = ReadOnly` hozzárendeléssel. Az OAuth 2.1 (felhasználó nevében cselekvő AI)
   v2.0-ra halasztva (függ a Kerberos/OAuth migrációtól, ADR-002).

4. **Csak olvasás v1.0-ban** – `ExposeWriteTools = false` alapértelmezés. Írási tool-ok
   csak explicit engedélyezés + megfelelő szerepkör esetén, jövőbeli kiadásban.

5. **Feature flag mögött** – `Features:Mcp:Enabled = false` alapértelmezés. Ha le van tiltva,
   az endpoint nincs map-elve és az SDK nem tölt be — nulla overhead és támadási felület.

6. **Stateless mód** – K8s horizontális skálázáshoz (P5 alapelv); session-affinitás nélkül
   bármely pod kiszolgálhatja a kérést.

---

## Következmények

**Előnyök:**

- AI-asszisztensek közvetlenül kérdezhetik le az inventory-t természetes nyelven
- Egységes adathozzáférés: REST és MCP ugyanazon adatot adja vissza, ugyanolyan szűréssel
- Olcsó bekapcsolás: `Features:Mcp:Enabled = true` + service token
- Audit log rögzíti az AI-kéréseket, így látható, mit kérdez az AI

**Kockázatok és mitigáció:**

- *Token kompromittálódás* → Token rotation K8s Secret-tel; rövid érvényességi idő v2.0-ban
- *Adatszivárgás* → RBAC query-szinten, ReadOnly tokennel csak publikus adat és User dok
- *Párhuzamos logika csúszása* → Code review: toolset-ek nem tartalmazhatnak új üzleti logikát

---

## Elutasított alternatívák

### 1. Csak REST API + AI "function calling"

Az AI-kliens REST endpoint-okat hív `tool_use` payloaddal. Fragilis: az AI-nak ismernie kell
az API struktúrát, hibakezelés nehezebb, sémaváltozáskor az AI-konfiguráció is frissül.

### 2. RAG pipeline (vektor-adatbázis)

Az inventory tartalmát vektor-adatbázisba indexeljük, az AI szemantikus keresést végez.
Költségesebb infrastruktúra, aszinkron frissítési ciklus, konzisztencia-problémák. MCP
real-time adatot ad, nem snapshot-ot. Választható kiegészítésként v3.0-ban.

### 3. GraphQL endpoint

Rugalmasabb lekérdezés, de az AI-kliens szintén ismernie kell a sémát. Az MCP tool-definíció
sokkal AI-natívabb és könnyebben menthető a kliens kontextusában.
