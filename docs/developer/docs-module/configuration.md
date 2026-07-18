# Documentation Module – Configuration

## Feature flag

```json
{
  "Features": {
    "Documentation": {
      "Enabled": true
    }
  }
}
```

`Enabled: false` esetén minden `/api/v1/applications/{id}/docs` végpont 501 Not Implemented választ ad.

## Tartalom méretkorlát

500 KB/dokumentum (UTF-8 byte-ban mérve). A limit kizárólag szerver-oldalon érvényesül a controllerben; kliens-oldali jelzés a frontend `DocumentationEdit` oldalon van (issue #17).

## Megőrzési idő (verzióhistory)

A jelenlegi implementációban a history-bejegyzések korlátlan ideig megmaradnak. A 90 napos törlési szabály (spec 10.3) egy background cleanup jobot igényel (v2.0).

## Adatbázis-migráció

| Fájl | Tartalom |
|------|---------|
| `src/.../Migrations/202507161300_Docs_DocumentationAndHistory.cs` | EF Core migráció |
| `sql/migrations/202507161300_Docs_DocumentationAndHistory.sql` | PostgreSQL DDL |

## DI regisztráció

A modul az `AppInventoryDbContext`-en keresztül érhető el, önálló DI regisztrációt nem igényel. A feature flag a `FeatureGate("Documentation")` action filterrel van bekötve a controlleren.
