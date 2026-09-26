#!/usr/bin/env sh
set -eu

container_name="${SQLSERVER_CONTAINER_NAME:-micro-insurtech-sql}"
sa_password="${SQLSERVER_SA_PASSWORD:-Change_this_password_123!}"
database_name="${SQLSERVER_DATABASE_NAME:-MicroInsurTech}"
repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)

case "$database_name" in
    ''|[0-9]*|*[!A-Za-z0-9_]*)
        echo "SQLSERVER_DATABASE_NAME must be an identifier containing letters, digits, and underscores, starting with a letter or underscore." >&2
        exit 1
        ;;
esac

if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required to apply the local SQL schema." >&2
    exit 1
fi

if ! docker ps --format '{{.Names}}' | grep -qx "$container_name"; then
    echo "SQL Server container '$container_name' is not running. Run scripts/start-local-sqlserver.sh first." >&2
    exit 1
fi

docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$sa_password" \
    -C \
    -b \
    -Q "IF DB_ID('$database_name') IS NULL CREATE DATABASE [$database_name];"

table_count=$(docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$sa_password" -C -b -h -1 -W \
    -d "$database_name" \
    -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name IN ('Clients', 'Properties', 'Quotes');" | tr -d '[:space:]')

case "$table_count" in
    3)
        echo "Quote tables already exist in '$database_name'; preserving existing records."
        exit 0
        ;;
    0) ;;
    *)
        echo "Database '$database_name' has an incomplete quote schema. Resolve it before initializing." >&2
        exit 1
        ;;
esac

docker cp "$repo_root/database/schema.sql" "$container_name:/tmp/schema.sql"

docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$sa_password" \
    -C \
    -b \
    -d "$database_name" \
    -i /tmp/schema.sql

echo "Applied database/schema.sql to database '$database_name'."
