#!/usr/bin/env sh
set -eu

container_name="${SQLSERVER_CONTAINER_NAME:-micro-insurtech-sql}"
sa_password="${SQLSERVER_SA_PASSWORD:-Change_this_password_123!}"
database_name="${SQLSERVER_DATABASE_NAME:-MicroInsurTech}"

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
    -Q "IF DB_ID('$database_name') IS NULL CREATE DATABASE [$database_name];"

docker cp database/schema.sql "$container_name:/tmp/schema.sql"

docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$sa_password" \
    -C \
    -d "$database_name" \
    -i /tmp/schema.sql

echo "Applied database/schema.sql to database '$database_name'."
