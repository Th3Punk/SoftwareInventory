# Documentation Module – Overview

## Cél

Az alkalmazás-katalógus dokumentációs alrendszere: Markdown-alapú dokumentumok létrehozása, szerkesztése és verziókövetése alkalmazásonként.

## Főbb osztályok

| Osztály | Felelősség |
|---------|-----------|
| `Documentation` | Fő dokumentum entitás (tartalom, típus, státusz, verzió) |
| `DocumentationHistory` | Korábbi verziók archívuma |
| `DocumentationsController` | 8 HTTP végpont (CRUD + státusz + history) |

## Architektúra

```
GET /api/v1/applications/{id}/docs        → lista (típus-láthatóság alapján szűrt)
GET /api/v1/applications/{id}/docs/{d}    → tartalom (jogosultság-ellenőrzéssel)
POST /api/v1/applications/{id}/docs       → létrehozás [Developer, Admin]
PUT  /api/v1/applications/{id}/docs/{d}   → szerkesztés + automatikus verzióarchivál
PATCH /api/v1/applications/{id}/docs/{d}/status  → státuszváltás
DELETE /api/v1/applications/{id}/docs/{d} → archivál [Admin]
GET /api/v1/applications/{id}/docs/{d}/history   → verzióhistory [Developer, Admin]
GET /api/v1/applications/{id}/docs/{d}/history/{v} → adott verzió tartalma
```

## Típus-láthatóság (spec 10.1)

| Típus | Látja |
|-------|-------|
| `User` | Minden authentikált felhasználó |
| `Developer` | Developer + Admin szerepkör |
| `Operations` | Kizárólag Admin |

## Verziókövetés

Minden PUT híváskor:

1. Az aktuális tartalom (`Content`, `Version`) bekerül a `DocumentationHistories` táblába.
2. A `Version` mező inkrementálódik.
3. History-bejegyzések 90 napig olvashatók (cleanup background jobban – v2.0).

## Feature flag

`Features:Documentation:Enabled = false` → minden endpoint 501-et ad vissza.
