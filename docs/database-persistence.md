# Database Persistence

The core engine persists every valid quote request after quote calculation and before returning the API response.

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

- `FirstName`
- `LastName`
- `Email`
- `CreatedAt`

### `Properties`

Stores the submitted property details and the resolved postcode region when available:

- `ClientID`
- `Postcode`
- `Region`
- `YearBuilt`
- `RebuildCost`
- `IsUnoccupied`

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

The schema is maintained in `database/schema.sql`. Apply that SQL to SQL Server or Azure SQL before running quote requests against the API.
