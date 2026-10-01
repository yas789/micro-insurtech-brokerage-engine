#!/usr/bin/env sh
set -eu

container_name="${SQLSERVER_CONTAINER_NAME:-micro-insurtech-sql}"
sa_password="${SQLSERVER_SA_PASSWORD:-Change_this_password_123!}"
host_port="${SQLSERVER_HOST_PORT:-1433}"

if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required to start local SQL Server." >&2
    exit 1
fi

if docker ps --format '{{.Names}}' | grep -qx "$container_name"; then
    echo "SQL Server container '$container_name' is already running."
elif docker ps -a --format '{{.Names}}' | grep -qx "$container_name"; then
    docker start "$container_name" >/dev/null
else
    docker run \
        --name "$container_name" \
        --detach \
        --platform linux/amd64 \
        --env ACCEPT_EULA=Y \
        --env MSSQL_SA_PASSWORD="$sa_password" \
        --publish "$host_port:1433" \
        mcr.microsoft.com/mssql/server:2022-latest >/dev/null
fi

echo "Waiting for SQL Server to accept connections..."

attempt=1
while [ "$attempt" -le 60 ]; do
    if docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd \
        -S localhost \
        -U sa \
        -P "$sa_password" \
        -C \
        -b \
        -Q "SELECT 1" >/dev/null 2>&1; then
        echo "SQL Server is ready on localhost:$host_port."
        exit 0
    fi

    attempt=$((attempt + 1))
    sleep 2
done

echo "SQL Server did not become ready in time." >&2
exit 1
