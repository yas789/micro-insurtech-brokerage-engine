# Architecture

## Overview

The Micro-InsurTech Brokerage Engine is a vertical slice system for commercial property insurance quote broking. It mirrors a broker platform where a public web interface captures risk information, a gateway backend normalizes and forwards submissions, and a core underwriting engine coordinates rating, enrichment, persistence, and quote return.

## Architecture Layers

### 1. Frontend UI Layer

Responsibilities:

- Render a professional multi-step quote form.
- Collect client and property details.
- Submit quote requests asynchronously to the PHP gateway.
- Display loading, validation, error, and result states.
- Render returned quotes in ascending premium order.
- Provide an `Accept Cover` action for each quote option.

Technology constraints:

- Semantic HTML5.
- CSS3 or Tailwind CSS.
- Native modern JavaScript using `fetch()`.
- No direct calls to the .NET core engine.

### 2. PHP Gateway Layer

Responsibilities:

- Act as the browser-facing backend.
- Receive frontend submissions.
- Validate and normalize incoming request payloads.
- Call the .NET core engine over HTTP.
- Handle network, timeout, validation, and upstream server errors cleanly.
- Return safe JSON responses to the browser.

Technology constraints:

- PHP 8.x.
- Object-oriented service class such as `BrokerService`.
- Native `curl` or Guzzle for outbound HTTP.
- MVC-style separation between request controller and API client service.

### 3. C# Core Processing Engine Layer

Responsibilities:

- Expose quote calculation API endpoints.
- Validate property and client input.
- Enrich submitted postcode data using `https://postcodes.io`.
- Execute underwriting schemes concurrently with `Task.WhenAll`.
- Persist client, property, and quote records before responding.
- Return a quote response array suitable for UI comparison.

Technology constraints:

- .NET 8 Web API.
- Strict object-oriented design.
- `IUnderwriterService` abstraction.
- Three concrete underwriter scheme classes:
  - `AvivaScheme`
  - `AXAScheme`
  - `NichePropertyCover`
- Entity Framework Core, Dapper, or a lightweight repository pattern.

### 4. Data Persistence Layer

Responsibilities:

- Store client identity details.
- Store insured property characteristics.
- Store generated quote records.
- Enforce primary key and foreign key relationships.
- Use safe data types for financial and user-input fields.

Technology constraints:

- SQL Server or Azure SQL.
- Explicit primary keys.
- Identity constraints.
- Explicit foreign keys.
- Financial values stored as `DECIMAL(10,2)`.

## Request Flow

```text
1. User completes the frontend quote journey.
2. Browser submits JSON to the PHP gateway using fetch().
3. PHP gateway validates the request and posts JSON to the .NET API.
4. .NET API validates and maps the request into DTOs.
5. .NET API calls postcodes.io to enrich postcode region data.
6. .NET API invokes all underwriter services concurrently.
7. .NET API persists client, property, and quote records.
8. .NET API returns generated quote results to PHP.
9. PHP gateway returns sanitized JSON to the browser.
10. Browser renders quote comparison from lowest to highest premium.
```

## Security Boundaries

- Browser only communicates with the PHP gateway.
- Internal .NET API base URL is configured server-side only.
- Database credentials are never exposed to frontend or PHP templates.
- Secrets must be supplied through environment variables or managed cloud secret stores.
- All external input must be validated at the PHP gateway and again at the .NET API boundary.
- Production deployments must use HTTPS between all public and private service boundaries.
