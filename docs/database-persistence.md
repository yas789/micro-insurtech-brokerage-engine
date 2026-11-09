# Database Persistence

The core engine persists every valid quote request after quote calculation and before returning the API response.

The API validates SQL-backed request limits before quote calculation, so over-limit client or property values return `400 Bad Request` instead of failing during database persistence.

## Save Flow

When `POST /api/quotes` receives a valid request:

1. The controller validates the client and property payload.
2. The orchestration service enriches the postcode and calculates underwriter quotes.
3. The persistence service opens a database transaction when the database provider supports transactions.
4. The persistence service inserts one `Clients` row.
5. The persistence service inserts one linked `Properties` row.
6. The persistence service inserts one linked `Quotes` row for each returned underwriter quote.
7. The transaction commits.
8. The API returns the quote response to the caller.

If persistence fails, the API request fails instead of returning quotes that were not saved.

## Tables Written

### `Clients`

Stores normalized client identity fields:

- `FirstName`, maximum `100` characters.
- `LastName`, maximum `100` characters.
- `Email`, maximum `254` characters.
- `CreatedAt`

### `Properties`

Stores the submitted property details and the resolved postcode region when available:

- `ClientID`
- `Postcode`, maximum `16` characters.
- `Region`
- `YearBuilt`
- `RebuildCost`, from `0.01` to `99999999.99`, with at most two decimal places.
- `IsUnoccupied`
- `QuoteReference`, application-generated random UUID with a unique index.
- `CreatedAt`, second-precision UTC timestamp returned only after commit.

The frontend mirrors the `DECIMAL(10,2)` range and penny precision. Both the PHP
gateway and core API reject costs above the maximum or with fractional pennies
before rating or persistence, returning `400` and a validation message. Values
are rejected rather than silently rounded, including values that would round
beyond the SQL maximum or down to zero.

### `Quotes`

Stores one row per underwriter result:

- `PropertyID`
- `UnderwriterName`
- `PremiumAmount`
- `RiskRating`
- `Region`
- `GeneratedAt`

## Configuration

The core engine requires the `BrokerDatabase` connection string:

```text
ConnectionStrings__BrokerDatabase=Server=localhost,1433;Database=MicroInsurTech;User Id=sa;Password=<password>;TrustServerCertificate=True;
```

The fresh schema is maintained in `database/schema.sql` (a destructive rebuild).
Upgrade existing databases with `database/upgrades/001-saved-comparisons.sql`
before deploying this API version. It retains records and assigns legacy
comparisons random references and the linked client's creation time. The local
initializer applies this upgrade automatically. Do not use the rebuild script
to upgrade a populated database.

Retrieval uses the public property reference, with a no-tracking projection that
excludes client identity/internal IDs and returns saved quotes in premium order.
It creates no new rows and never recalculates prices. Removing browser history
does not delete database rows. See `docs/quote-history.md`.
