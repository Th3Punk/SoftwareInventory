# Documentation Module – Troubleshooting

## 501 Not Implemented minden végponton

**Ok:** `Features:Documentation:Enabled = false` a konfigurációban.
**Megoldás:** `appsettings.json`-ban vagy environment variable-ban `Features__Documentation__Enabled=true`.

## 403 Forbidden dokumentum olvasásakor

**Ok:** A felhasználónak nincs megfelelő szerepköre az adott `DocumentationType`-hoz:

- `Developer`/`Operations` típushoz Developer vagy Admin szükséges.
- `Operations` típushoz kizárólag Admin.

**Megoldás:** Ellenőrizd a felhasználó `UserRole` bejegyzéseit a `Users`/`UserRoles` táblában.

## PUT frissítés nem ment history-t

**Ok:** Az `AppInventory.Tests` projekt InMemory adatbázisa nem triggereli az SQL constraint-eket; éles PostgreSQL-en az index ellenőrzés érvényesül.

**Megoldás:** Ellenőrizd, hogy a `DocumentationHistories` táblában szerepel-e a `(DocumentationId, Version)` unique index (`IX_DocumentationHistories_DocumentationId_Version`). Ha nem, futtasd az `202507161300_Docs_DocumentationAndHistory.sql` migrációt.

## Content túl nagy (400 Bad Request)

**Ok:** A dokumentum tartalma meghaladja az 500 KB-ot (UTF-8).
**Megoldás:** Rövidítsd a tartalmat, vagy oszd több dokumentumra.

## History-bejegyzés nem olvasható (404)

**Ok:** A kért verzió nem létezik a `DocumentationHistories` táblában — az aktuális verzió a `Documentations` táblában van, a history csak a korábbi verziókat tartalmazza.
