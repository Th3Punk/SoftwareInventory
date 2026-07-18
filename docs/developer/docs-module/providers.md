# Documentation Module – Providers

## Tárolás

A dokumentumok tartalma (`Content`) Markdown szövegként kerül az adatbázisba (`text` típusú PostgreSQL oszlop). A rendszer nem tárol bináris tartalmat; képek külső URL-ként hivatkozhatók.

## IDocumentStore (jövőbeli provider)

A CLAUDE.md pluggable architektúra szerint az `IDocumentStore` interfész egy cserélhető tároló provider. A v1.0-ban az implementáció közvetlenül az EF Core `AppInventoryDbContext`-et használja (Database Document Store).

| Provider | Leírás |
|----------|--------|
| `DatabaseDocumentStore` | Alapértelmezett – PostgreSQL-ben tárolja a tartalmat |
| `NullDocumentStore` | Feature kikapcsolt esetén |

## Provider csere

A jövőbeli `IDocumentStore`-alapú implementációhoz az `Add{DocumentStore}Provider` DI extension minta alkalmazandó (spec 5.2).
