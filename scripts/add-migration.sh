#!/usr/bin/env bash
# ============================================================================
# add-migration.sh — Dual-Provider Migration Generator
# ============================================================================
# Generates EF Core migrations for BOTH SQLite and PostgreSQL in one shot.
#
# Usage:
#   ./scripts/add-migration.sh <MigrationName>
#
# Example:
#   ./scripts/add-migration.sh AddUserPreferences
#
# Both providers share the same assembly (Winnow.Server), so migration names
# are suffixed to avoid collisions:
#   - SQLite:   <Name>         → Migrations/Sqlite
#   - Postgres: <Name>Pg       → Migrations/Postgres/
#
# IMPORTANT: Because both providers share the same DbContext, EF Core uses
# a single model snapshot. To generate clean CreateTable migrations for each
# provider, we must temporarily hide the other provider's migration directory
# so EF Core doesn't diff against the wrong snapshot.
#
# The script uses the DatabaseProvider env var override, which the
# WinnowDbContextFactory reads via AddEnvironmentVariables().
# ============================================================================
set -euo pipefail

MIGRATION_NAME="${1:?❌ Usage: $0 <MigrationName>}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SERVER_DIR="$REPO_ROOT/src/Services/Winnow.Server"

echo "============================================"
echo " Dual-Provider Migration: $MIGRATION_NAME"
echo "============================================"
echo ""

# ── Step 1: PostgreSQL ──────────────────────────────────────────────────────
echo "▶ Generating PostgreSQL migration..."
(
  cd "$SERVER_DIR"

  DatabaseProvider=Postgres dotnet ef migrations add "${MIGRATION_NAME}Pg" \
    --output-dir Migrations/Postgres \
    --namespace Winnow.Server.Migrations.Postgres
)
echo "✅ PostgreSQL migration created."
echo ""

echo "============================================"
echo " ✅ Migration generated successfully!"
echo "============================================"
echo ""
echo "  Postgres: Migrations/Postgres/${MIGRATION_NAME}Pg"
echo ""
echo "Review the generated files, then commit."
