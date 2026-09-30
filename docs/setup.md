# Setup Guide

This document describes the local setup model. The .NET core engine has a health endpoint, quote API, postcode enrichment, and SQL persistence. The PHP gateway forwards browser-facing quote requests to the core engine. The frontend quote journey submits through the PHP gateway.

## Local Development Topology

Recommended local ports:

- Frontend static server: `http://localhost:3000`
- PHP gateway: `http://localhost:8080`
- .NET core engine: `http://localhost:5000`
- SQL Server: `localhost,1433`

## Environment Variables

### PHP Gateway

```text
BROKER_CORE_API_BASE_URL=http://localhost:5000
BROKER_CORE_API_TIMEOUT_SECONDS=15
APP_ENV=local
```

### .NET Core Engine

```text
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__BrokerDatabase=Server=localhost,1433;Database=MicroInsurTech;User Id=sa;Password=<password>;TrustServerCertificate=True;
POSTCODES_API_BASE_URL=https://postcodes.io
```

## Core Engine Local Run

Apply `database/schema.sql` to SQL Server first, then provide `ConnectionStrings__BrokerDatabase` before sending quote requests. The API starts only when the `BrokerDatabase` connection string is configured.
See `docs/database-persistence.md` for the exact records written by a valid quote request.

Prerequisites for local end-to-end verification:

- Docker with Linux containers available on PATH.
- .NET 8 SDK/runtime available on PATH.
- Local ports `1433` and `5000` available.

Start local SQL Server in Docker:

```text
SQLSERVER_SA_PASSWORD=Change_this_password_123! scripts/start-local-sqlserver.sh
```

Create the local database and apply `database/schema.sql`:

```text
SQLSERVER_SA_PASSWORD=Change_this_password_123! scripts/apply-local-schema.sh
```

The initializer creates quote tables only when all three are absent. Re-running
it preserves existing records; a partial schema is reported as an error. Database
names must be simple SQL identifiers. The script resolves the schema relative to
its own location and fails on SQL errors. `database/schema.sql` itself remains a
destructive rebuild script, not a migration; do not use it to upgrade saved data.
The SQL startup helper waits for readiness even when the container is already running.

Export the core engine configuration:

```text
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__BrokerDatabase='Server=localhost,1433;Database=MicroInsurTech;User Id=sa;Password=Change_this_password_123!;TrustServerCertificate=True;'
export POSTCODES_API_BASE_URL=https://postcodes.io
```

From the repository root:

```text
dotnet run --project core-engine/MicroInsurTech.CoreEngine.csproj --urls http://localhost:5000
```

Useful endpoints:

- `GET /health`
- `POST /api/quotes`

In a second shell, run the backend verification script:

```text
SQLSERVER_SA_PASSWORD=Change_this_password_123! scripts/verify-core-engine.sh
```

That script checks `/health`, sends a valid quote request, and prints persisted row counts from SQL Server.

## PHP Gateway Local Run

With the core engine running, the recommended way to launch the two browser-facing
processes together is:

```sh
BROKER_CORE_API_BASE_URL=http://localhost:5000 python3 scripts/start-local-web.py
```

This requires PHP 8.1+ with `curl` and Python 3.10+. It checks gateway health,
serves the frontend at `http://localhost:3000`, and stops both children on Ctrl-C
or if either process exits. `GATEWAY_PORT` and `FRONTEND_PORT` override local ports;
the launcher connects the proxy to the gateway port automatically. SQL Server
and the core engine are managed separately. Configuration is read from exported
environment variables; `.env.example` is a reference, not an automatically loaded file.

To run the gateway separately, set its variables and use the entrypoint as the
built-in PHP server router from the repository root:

```text
BROKER_CORE_API_BASE_URL=http://localhost:5000 php -S 127.0.0.1:8080 -t php-gateway/public php-gateway/public/index.php
```

Useful endpoints:

- `GET /health`
- `POST /api/quotes`

The gateway validates browser-facing JSON, normalizes scalar request values, forwards valid quote requests to the .NET core engine, and converts upstream failures into safe JSON errors.

Run PHP gateway tests from the gateway directory:

```text
composer install
composer test
```

## Frontend Local Run

Start the PHP gateway above, then serve the frontend with its local proxy from
the repository root (Python 3.10+):

```text
python3 scripts/serve-frontend.py
```

Open `http://localhost:3000`. The server serves browser assets and proxies
`/api/*` and `/health` to PHP at `http://127.0.0.1:8080`. Override `FRONTEND_PORT`
or `GATEWAY_BASE_URL` when needed. The frontend uses same-origin `/api/quotes`;
no browser CORS setup or internal .NET URL is needed. This Python server is a
local development tool; production hosting must provide equivalent routing.

Verify the proxy with `python3 -m unittest discover -s scripts/tests`.

Run frontend unit tests from the frontend directory:

```text
npm ci
npm test
```

### Azure Deployment

Recommended cloud configuration:

- Azure App Service for PHP gateway.
- Azure App Service or Azure Container Apps for .NET core engine.
- Azure SQL Database for persistence.
- Azure Key Vault for connection strings and secrets.
- Application Insights for telemetry.

Example Azure-oriented variables:

```text
BROKER_CORE_API_BASE_URL=https://<core-engine-app>.azurewebsites.net
ConnectionStrings__BrokerDatabase=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Authentication=Active Directory Default;Encrypt=True;
POSTCODES_API_BASE_URL=https://postcodes.io
```

## Planned Build Sequence

1. Create SQL Server schema.
2. Build .NET 8 core engine with DTOs, services, controller, persistence, and postcode integration.
3. Build PHP gateway controller and `BrokerService` API client.
4. Build frontend multi-step form and quote matrix.
5. Add local integration testing documentation.
6. Add deployment documentation for Azure.

## Security Setup Notes

- Store database credentials outside source control.
- Prefer managed identity in Azure when possible.
- Restrict SQL firewall access to known service identities or networks.
- Apply HTTPS-only policies in production.
- Configure CORS so the browser only talks to the PHP gateway.
- Do not expose the .NET core engine publicly unless it is protected by authentication, network restrictions, or API gateway policy.

## Operational Checks To Add Later

- Database migration verification.
- Health endpoint for .NET API.
- PHP gateway upstream connectivity check.
- Postcode service timeout and retry policy.
- Structured logs with correlation IDs.
