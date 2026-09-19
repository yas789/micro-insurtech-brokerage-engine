# Micro-InsurTech Brokerage Engine

Project scaffold for a production-oriented vertical slice that models the technical infrastructure of an independent commercial property insurance broker.

The repository currently contains documentation, layer folders, SQL schema, core quote contracts, underwriter scheme calculations, the first quote API endpoint, postcode enrichment, and SQL persistence wiring. PHP forwarding and frontend quote flow are still pending.

## Purpose

The Micro-InsurTech Brokerage Engine is designed as a four-layer, fully decoupled insurance brokerage platform:

1. Frontend UI Layer: Semantic HTML5, CSS3 or Tailwind CSS, and native ES6+ JavaScript.
2. PHP Gateway Layer: Object-oriented PHP 8.x MVC-style web backend that receives browser requests and calls the core API.
3. Core Processing Engine Layer: C# .NET 8 Web API implementing underwriting workflows, external postcode enrichment, concurrent quote calculation, and persistence.
4. Data Persistence Layer: SQL Server or Azure SQL relational database for clients, properties, quotes, and operational records.

## Project Structure

```text
micro-insurtech-brokerage-engine/
  .env.example
  .gitignore
  README.md
  database/
    schema.sql
  core-engine/
    MicroInsurTech.CoreEngine.csproj
    Program.cs
    Controllers/
    Data/
    Dtos/
    Schemes/
    Services/
  php-gateway/
    composer.json
    public/
      index.php
    src/
      BrokerService.php
  frontend/
    index.html
    app.js
    styles.css
  core-engine-tests/
    MicroInsurTech.CoreEngine.Tests.csproj
    Schemes/
    Services/
  docs/
    architecture.md
    specification.md
    setup.md
    implementation-roadmap.md
    database-persistence.md
```

## Layer Communication

```text
Browser UI
  -> PHP Gateway
    -> .NET 8 Core Engine API
      -> postcodes.io
      -> SQL Server / Azure SQL
```

The browser must not call the .NET core engine directly. The PHP gateway owns browser-facing request handling and shields internal service URLs, credentials, and transport details.

## Core Deliverables Planned

- `schema.sql`: SQL Server schema for `Clients`, `Properties`, and `Quotes`.
- `.NET 8 Web API`: Quote orchestration, underwriting schemes, postcode enrichment, and database persistence.
- `PHP 8.x Gateway`: Object-oriented broker service that forwards clean JSON payloads to the .NET API.
- `Frontend`: Multi-step quote form with asynchronous submission and quote comparison matrix.
- Operational documentation: Setup, environment variables, deployment notes, and security guidance.

## Engineering Principles

- Fully decoupled layers.
- Object-oriented service boundaries.
- Secure-by-default configuration.
- Async-first core processing.
- Explicit relational integrity in the database.
- Minimal browser exposure of internal APIs.
- Cloud-portable local configuration for Azure-hosted services.

## Current Status

Core API slice implemented: SQL schema, C# DTOs, `IUnderwriterService`, three underwriter schemes, `POST /api/quotes`, postcode region lookup, concurrent quote orchestration, EF Core SQL persistence, and unit tests. PHP gateway and frontend implementation are pending.
