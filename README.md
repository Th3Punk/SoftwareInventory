# SoftwareInventory (AppInventory)

Belső szoftverleltár- és dokumentáció-kezelő rendszer ASP.NET Core 9 + PostgreSQL alapokon.

## Előfeltételek

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [Node.js 20+](https://nodejs.org/) (frontend fejlesztéshez)

## Gyorsindítás

```bash
# 1. Repo klónozása
git clone <repo-url>
cd SoftwareInventory

# 2. Adatbázis létrehozása
createdb appinventory_dev

# 3. Connection string beállítása (opcionális – a fejlesztői default localhost-ot használ)
# Ha szükséges, másold és szerkeszd: appsettings.Development.json

# 4. Backend indítás
dotnet restore
dotnet run --project src/AppInventory.Api

# Az API elérhető: https://localhost:5001
# Scalar API dokumentáció: https://localhost:5001/scalar/v1
```

## Projekt-struktúra

```
AppInventory.sln
src/
├── AppInventory.Core/              # Domain entitások, interfészek (függőségmentes)
├── AppInventory.Infrastructure/    # EF Core, provider implementációk
├── AppInventory.Api/               # ASP.NET Core host, controllerek, DI
└── AppInventory.Tests/             # Unit és integrációs tesztek

sql/migrations/                     # Generált PostgreSQL DDL szkriptek
docs/                               # Fejlesztői, felhasználói és üzemeltetési dokumentáció
```

## Rétegzés

| Réteg            | Függőség                |
| ---------------- | ----------------------- |
| Core             | Semmi (tiszta domain)   |
| Infrastructure   | Core                    |
| Api              | Core, Infrastructure    |
| Tests            | Core, Infrastructure, Api |

## Tesztek futtatása

```bash
dotnet test
```

## Dokumentáció

- [Rendszerspecifikáció](docs/specification.md)
- [Fejlesztői dokumentáció](docs/developer/)
- [Felhasználói dokumentáció](docs/user/)
- [Üzemeltetési dokumentáció](docs/ops/)
- [Architektúra döntések (ADR)](docs/adr/)

## Licenc

Lásd [LICENSE](LICENSE).
