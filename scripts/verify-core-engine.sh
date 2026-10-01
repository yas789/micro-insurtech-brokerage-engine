#!/usr/bin/env sh
set -eu

api_base_url="${CORE_ENGINE_BASE_URL:-http://localhost:5000}"
container_name="${SQLSERVER_CONTAINER_NAME:-micro-insurtech-sql}"
sa_password="${SQLSERVER_SA_PASSWORD:-Change_this_password_123!}"
database_name="${SQLSERVER_DATABASE_NAME:-MicroInsurTech}"

if ! command -v curl >/dev/null 2>&1; then
    echo "curl is required to verify the core engine." >&2
    exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required to verify SQL persistence." >&2
    exit 1
fi

echo "Checking core engine health..."
curl --fail --silent --show-error "$api_base_url/health" >/dev/null

echo "Submitting quote request..."
curl --fail --silent --show-error \
    --request POST \
    --header "Content-Type: application/json" \
    --data '{"client":{"firstName":"Jane","lastName":"Broker","email":"jane@example.com"},"property":{"postcode":"SW1A 1AA","yearBuilt":1910,"rebuildCost":750000,"isUnoccupied":false}}' \
    "$api_base_url/api/quotes" >/tmp/micro-insurtech-quote-response.json

echo "Checking persisted row counts..."
docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$sa_password" \
    -C \
    -d "$database_name" \
    -Q "SELECT COUNT(*) AS Clients FROM dbo.Clients; SELECT COUNT(*) AS Properties FROM dbo.Properties; SELECT COUNT(*) AS Quotes FROM dbo.Quotes;"

echo "Core engine verification completed. Quote response saved to /tmp/micro-insurtech-quote-response.json."
