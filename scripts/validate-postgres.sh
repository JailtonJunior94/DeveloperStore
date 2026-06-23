#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

export POSTGRES_DB="${POSTGRES_DB:-developerstore_test}"
export POSTGRES_USER="${POSTGRES_USER:-developerstore_test}"
export POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-developerstore_test}"
export POSTGRES_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

cleanup() {
  docker compose down --volumes --remove-orphans >/dev/null 2>&1 || true
}

trap cleanup EXIT

docker compose up -d developerstore.db

for attempt in {1..30}; do
  if docker compose exec -T developerstore.db pg_isready -d "$POSTGRES_DB" -U "$POSTGRES_USER" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

docker compose exec -T developerstore.db pg_isready -d "$POSTGRES_DB" -U "$POSTGRES_USER" >/dev/null

dotnet test tests/DeveloperStore.Postgres/DeveloperStore.Postgres.csproj
