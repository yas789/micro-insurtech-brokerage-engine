# Implementation Roadmap

## Phase 0: Scaffold

Status: Complete.

Deliverables:

- Repository documentation.
- Layer directories for database, .NET core engine, PHP gateway, and frontend.
- Environment variable example file.
- Placeholder project files for each layer.

## Phase 1: Data Layer

Status: Complete.

Deliverables:

- `database/schema.sql`
- SQL Server tables for `Clients`, `Properties`, and `Quotes`.
- Primary keys, identity columns, foreign keys, and safe data types.

Acceptance criteria:

- Schema applies cleanly to SQL Server or Azure SQL.
- Foreign key integrity prevents orphaned properties or quotes.
- Financial values use `DECIMAL(10,2)`.

## Phase 2: Core Engine

Status: Partially complete.

Deliverables:

- .NET 8 Web API project.
- `IUnderwriterService` interface.
- `AvivaScheme`, `AXAScheme`, and `NichePropertyCover` implementations.
- Quote controller endpoint.
- Postcode enrichment client.
- Database persistence.

Completed:

- DTO contracts for quote requests, client details, property evaluation, and quote results.
- `IUnderwriterService` interface.
- Pure underwriter scheme implementations.
- Unit tests for scheme base and multiplier cases.
- `POST /api/quotes` controller endpoint.
- Postcode enrichment client using `HttpClient`.
- Quote orchestration service that runs underwriter schemes concurrently and returns quotes sorted by premium.
- Unit tests for quote orchestration behavior.

Remaining:

- Database persistence.

Acceptance criteria:

- Quote endpoint accepts client and property data.
- All three schemes execute concurrently.
- Postcode lookup is asynchronous and resilient.
- Client, property, and quote records are persisted before response.
- Response contains all generated quotes.

## Phase 3: PHP Gateway

Deliverables:

- `BrokerService.php`.
- `index.php` controller endpoint.
- JSON request validation and response handling.
- Upstream .NET API error handling.

Acceptance criteria:

- Frontend can submit to PHP without knowing the .NET API URL.
- Network and server errors return safe browser-facing JSON.
- Payloads are encoded and decoded safely.

## Phase 4: Frontend

Deliverables:

- `index.html`.
- `app.js`.
- Optional CSS or Tailwind configuration.

Acceptance criteria:

- Multi-step form captures all required fields.
- Submission uses native `fetch()`.
- Loading state communicates quote retrieval from three underwriters.
- Quote matrix renders options from lowest to highest premium.
- Each option includes an `Accept Cover` button.

## Phase 5: Hardening

Deliverables:

- Integration test notes.
- Security checklist.
- Deployment checklist.
- Observability guidance.

Acceptance criteria:

- No secrets in repository.
- Environment-specific configuration documented.
- Failures are handled at each layer boundary.
- Azure deployment path is clear.
