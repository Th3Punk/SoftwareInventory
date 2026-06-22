# SoftwareInventory – fejlesztői parancsok

compose_file := "docker-compose.dev.yml"

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
