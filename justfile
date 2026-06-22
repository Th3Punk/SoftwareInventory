# SoftwareInventory – fejlesztői parancsok
#
# Használat: just <parancs>
# Lista:     just --list

set dotenv-load := false

compose_file := "docker-compose.dev.yml"
api_project  := "src/AppInventory.Api"
test_project := "src/AppInventory.Tests"
solution     := "AppInventory.sln"

# ─── Build & Run ───────────────────────────────────────────────

# Solution build (Release)
build:
    dotnet build {{solution}} -c Release

# API futtatása (Development)
run:
    dotnet run --project {{api_project}}

# Tesztek futtatása coverage-dzsel
test:
    dotnet test {{solution}} --collect:"XPlat Code Coverage" --results-directory ./artifacts/coverage

# ─── Kódminőség ────────────────────────────────────────────────

# Backend formázás ellenőrzése
format-check:
    dotnet format {{solution}} --verify-no-changes --verbosity normal

# Backend formázás alkalmazása
format:
    dotnet format {{solution}}

# Backend lint (format verify + build warnings)
lint-backend: format-check build

# Frontend lint (ESLint + Prettier)
lint-frontend:
    cd frontend && npm run lint

# Teljes lint (backend + frontend)
lint: lint-backend lint-frontend

# Markdown lint a docs/ és gyökér md fájlokon
lint-md:
    npx markdownlint-cli2 "docs/**/*.md" "*.md"

# ─── Adatbázis & Migráció ─────────────────────────────────────

# EF Core migráció futtatása (database update)
migrate:
    dotnet ef database update --project src/AppInventory.Infrastructure --startup-project {{api_project}}

# Új migráció hozzáadása (just add-migration name=Auth_InitialSchema)
add-migration name:
    dotnet ef migrations add {{name}} --project src/AppInventory.Infrastructure --startup-project {{api_project}} --output-dir Data/Migrations

# PostgreSQL DDL szkript generálása az utolsó migrációból
gen-ddl:
    dotnet ef migrations script --idempotent --project src/AppInventory.Infrastructure --startup-project {{api_project}} --output sql/migrations/latest.sql

# ─── Docker Compose ────────────────────────────────────────────

# Secrets mappa és fájlok előkészítése helyi fejlesztéshez
setup-secrets:
    mkdir -p secrets
    @test -f secrets/db-password.txt || echo "dev_password" > secrets/db-password.txt
    @echo "Secrets kész."

# Docker Compose környezet indítása
up: setup-secrets
    docker compose -f {{compose_file}} up -d --build

# Docker Compose környezet leállítása
down:
    docker compose -f {{compose_file}} down

# Leállítás + volume-ok törlése (adatbázis reset)
down-clean:
    docker compose -f {{compose_file}} down -v

# Logok követése
logs service="api":
    docker compose -f {{compose_file}} logs -f {{service}}

# Adatbázis psql shell
db-shell:
    docker compose -f {{compose_file}} exec db psql -U appinventory appinventory_dev

# ─── OpenAPI & Frontend ────────────────────────────────────────

# OpenAPI JSON export (az API-nak futnia kell)
openapi:
    curl -s http://localhost:5000/openapi/v1.json | python3 -m json.tool > artifacts/openapi-v1.json
    @echo "Mentve: artifacts/openapi-v1.json"

# Frontend dev szerver indítása
fe-dev:
    cd frontend && npm run dev

# Frontend build
fe-build:
    cd frontend && npm run build

# ─── Git Hooks ─────────────────────────────────────────────────

# Git hook-ok telepítése (husky + commitlint + lint-staged)
hooks-install:
    npm install
    @echo "Git hook-ok telepítve."

# Git hook-ok eltávolítása
hooks-uninstall:
    git config --unset core.hooksPath || true
    @echo "Git hook-ok kikapcsolva."

# Commitlint ellenőrzés manuálisan (utolsó commit)
commitlint-last:
    npx commitlint --from HEAD~1 --to HEAD --verbose

# ─── Biztonság ─────────────────────────────────────────────────

# NuGet vulnerability scan (spec 16.5)
vuln-check:
    dotnet list {{solution}} package --vulnerable --include-transitive

# npm audit (ha van frontend)
vuln-check-frontend:
    cd frontend && npm audit
