# Setup Guide

This document describes the planned setup model. No runnable application code exists yet.

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
