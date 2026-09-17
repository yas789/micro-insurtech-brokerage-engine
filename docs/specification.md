# System Specification

## Business Domain

The platform supports commercial property quote generation for an independent insurance broker. A prospective client submits property details and receives a comparison of quotes from three simulated underwriter schemes.

## Data Model

### Clients

Fields:

- `ClientID`: Primary key, identity integer.
- `FirstName`: Required bounded text.
- `LastName`: Required bounded text.
- `Email`: Required bounded text, indexed candidate.
- `CreatedAt`: UTC creation timestamp.

Implemented constraints:

- `ClientID` is an identity primary key.
- Name and email fields must not be blank.
- `Email` has a supporting index.

### Properties

Fields:

- `PropertyID`: Primary key, identity integer.
- `ClientID`: Foreign key to `Clients.ClientID`.
- `Postcode`: Required bounded text.
- `YearBuilt`: Required integer.
- `RebuildCost`: Required `DECIMAL(10,2)`.
- `IsUnoccupied`: Required boolean or bit.

Implemented constraints:

- `PropertyID` is an identity primary key.
- `ClientID` is a foreign key to `Clients.ClientID`.
- `Postcode` must not be blank.
- `YearBuilt` must be between `1500` and `2100`.
- `RebuildCost` must be positive.

### Quotes

Fields:

- `QuoteID`: Primary key, identity integer.
- `PropertyID`: Foreign key to `Properties.PropertyID`.
- `UnderwriterName`: Required bounded text.
- `PremiumAmount`: Required `DECIMAL(10,2)`.
- `RiskRating`: Required bounded text or numeric score depending on implementation decision.
- `GeneratedAt`: UTC creation timestamp.

Implemented constraints:

- `QuoteID` is an identity primary key.
- `PropertyID` is a foreign key to `Properties.PropertyID`.
- `UnderwriterName` must not be blank.
- `PremiumAmount` must be positive.
- `RiskRating` must be `Low`, `Medium`, or `High`.

## Underwriting Contract

The core engine must define the following service contract:

```text
IUnderwriterService
  CalculatePremiumAsync(PropertyEvaluationDto property) -> Task<QuoteResult>
```

This contract is implemented in `core-engine/Services/IUnderwriterService.cs`.

## Underwriter Schemes

### AvivaScheme

Rules:

- Base premium: GBP 150.00.
- Risk multiplier: `1.4` when `YearBuilt < 1920`.
- Risk rating: `Medium` when multiplier applies, otherwise `Low`.

### AXAScheme

Rules:

- Base premium: GBP 180.00.
- Risk multiplier: `1.5` when `IsUnoccupied` is true.
- Risk rating: `High` when multiplier applies, otherwise `Low`.

### NichePropertyCover

Rules:

- Base premium: GBP 120.00.
- Risk multiplier: `1.3` when `RebuildCost > 500000.00`.
- Risk rating: `Medium` when multiplier applies, otherwise `Low`.

## External API Integration

The .NET core engine must call `postcodes.io` to enrich submitted postcode data.

Target service:

```text
https://postcodes.io
```

Required behavior:

- Use `HttpClient` via dependency injection.
- Perform asynchronous calls.
- Extract geographical region data when available.
- Handle failed, invalid, or unavailable postcode responses gracefully.
- Avoid blocking quote generation unless region data is a hard business requirement.

Implemented behavior:

- `PostcodesIoLookupService` calls `/postcodes/{postcode}` against `POSTCODES_API_BASE_URL`.
- Lookup failures are logged and return `null` region.
- Request cancellation is propagated.

## API Behavior

### Quote Request

Endpoint:

```text
POST /api/quotes
```

Expected logical payload:

```json
{
  "client": {
    "firstName": "Jane",
    "lastName": "Broker",
    "email": "jane@example.com"
  },
  "property": {
    "postcode": "SW1A 1AA",
    "yearBuilt": 1910,
    "rebuildCost": 750000.00,
    "isUnoccupied": false
  }
}
```

### Quote Response

Expected logical response:

```json
{
  "quotes": [
    {
      "underwriterName": "NichePropertyCover",
      "premiumAmount": 156.00,
      "riskRating": "Medium",
      "region": "London"
    }
  ]
}
```

The endpoint returns quotes ordered from lowest premium to highest premium.

Current persistence status:

- Quote calculation and postcode enrichment are implemented.
- Saving clients, properties, and quotes to SQL Server is pending the persistence slice.

## Validation Requirements

- `FirstName` and `LastName` are required.
- `Email` must be syntactically valid.
- `Postcode` is required and normalized before external lookup.
- `YearBuilt` must be a realistic year.
- `RebuildCost` must be positive.
- `IsUnoccupied` must be boolean.

## Non-Functional Requirements

- Production-ready configuration model.
- Layer-level decoupling.
- Async processing in the core engine.
- Resilient external API failure handling.
- Structured logging across PHP and .NET layers.
- Deterministic database writes before quote response.
- No secrets committed to source control.
