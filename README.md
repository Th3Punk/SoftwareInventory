# SoftwareInventory (AppInventory)

Belső szoftverleltár- és dokumentáció-kezelő rendszer ASP.NET Core 9 + PostgreSQL alapokon.

## Előfeltételek

- [Docker](https://docs.docker.com/get-docker/) + Docker Compose
- [just](https://github.com/casey/just) (opcionális, de ajánlott)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (helyi fejlesztéshez, Docker nélkül)
- [Node.js 20+](https://nodejs.org/) (frontend fejlesztéshez)

## Gyorsindítás (Docker)

```bash
# 1. Repo klónozása
git clone <repo-url>
cd SoftwareInventory

# 2. Indítás just-tal (secrets automatikusan létrejönnek)
just up

# VAGY Docker Compose-zal közvetlenül:
mkdir -p secrets && echo "dev_password" > secrets/db-password.txt
docker compose -f docker-compose.dev.yml up -d --build

# Az API elérhető: http://localhost:5000
# Scalar API dokumentáció: http://localhost:5000/scalar/v1
```

### Just parancsok

A `justfile` a fejlesztői parancsok egyetlen forrása. Teljes lista: `just --list`.

| Parancs                          | Leírás                                         |
| -------------------------------- | ---------------------------------------------- |
| **Build & Run**                  |                                                |
| `just build`                     | Solution build (Release)                       |
| `just run`                       | API futtatása (Development)                    |
| `just test`                      | Tesztek futtatása coverage-dzsel               |
| **Kódminőség**                   |                                                |
| `just format`                    | Backend kódformázás                            |
| `just format-check`              | Formázás ellenőrzése (CI-hoz)                  |
| `just lint`                      | Teljes lint (backend + frontend)               |
| `just lint-md`                   | Markdown lint (docs/)                          |
| **Adatbázis**                    |                                                |
| `just migrate`                   | EF Core migráció futtatása                     |
| `just add-migration name=...`    | Új migráció hozzáadása                         |
| `just gen-ddl`                   | PostgreSQL DDL szkript generálása              |
| **Docker**                       |                                                |
| `just up`                        | Környezet indítása (build + secrets setup)     |
| `just down`                      | Környezet leállítása                           |
| `just down-clean`                | Leállítás + adatbázis volume törlése           |
| `just logs`                      | API logok követése (`just logs db` a DB-hez)   |
| `just db-shell`                  | PostgreSQL psql shell                          |
| **OpenAPI & Frontend**           |                                                |
| `just openapi`                   | OpenAPI JSON export                            |
| `just fe-dev`                    | Frontend dev szerver                           |
| `just fe-build`                  | Frontend production build                      |
| **Biztonság**                    |                                                |
| `just vuln-check`                | NuGet vulnerability scan                       |
| `just vuln-check-frontend`       | npm audit                                      |

## Gyorsindítás (helyi, Docker nélkül)

```bash
# 1. PostgreSQL adatbázis létrehozása
createdb appinventory_dev

# 2. Connection string beállítása (opcionális – a fejlesztői default localhost-ot használ)
# Ha szükséges, másold és szerkeszd: appsettings.Development.json

# 3. Backend indítás
dotnet restore
dotnet run --project src/AppInventory.Api

# Az API elérhető: http://localhost:5000
# Scalar API dokumentáció: http://localhost:5000/scalar/v1
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
