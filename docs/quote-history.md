# Saved comparisons and browser history

## Feature contract

Each successful submission creates a saved comparison, represented by its existing
property row and linked quotes. It receives a random UUID v4 `quoteReference`
(never a sequential database ID) and UTC `createdAt`. Return metadata only after
persistence commits. Existing `quotes` remains unchanged in POST responses.

`POST /api/quotes` success:

```json
{
  "quoteReference": "3fc8045c-0a9a-4df4-96d6-f530bddb899a",
  "createdAt": "2026-11-08T12:00:00Z",
  "quotes": []
}
```

`GET /api/quotes/{reference}` success:

```json
{
  "quoteReference": "3fc8045c-0a9a-4df4-96d6-f530bddb899a",
  "createdAt": "2026-11-08T12:00:00Z",
  "property": {
    "postcode": "SW1A 1AA",
    "yearBuilt": 1910,
    "rebuildCost": 750000.00,
    "isUnoccupied": false,
    "region": "London"
  },
  "quotes": []
}
```

Retrieval returns the saved premiums and regions, ordered by premium then
underwriter name. It never calls postcode lookup, rating, or persistence. Empty
saved quote sets remain valid comparisons. Responses exclude names, email,
client IDs, property IDs, and quote database IDs. Invalid canonical UUID syntax
returns `400`; a well-formed but unknown reference returns `404`. Both layers use
safe JSON errors. UUID matching is case-insensitive; the all-zero UUID is invalid.

## Private links and local history

Browser links use `/#quote=<reference>`; the fragment is not sent when fetching
the page. Retrieval still goes through same-origin PHP `/api/quotes/{reference}`.
Possession of a link grants access to property details and prices: no account or
ownership check exists in this slice. Display that explanation next to links.
References must not be placed in analytics. Comparison responses use
`Cache-Control: no-store` through core, gateway, and local proxy.

Remember only reference and creation time in a versioned local-storage list,
newest first, deduplicated, capped at 20. Remember successful creation or opening;
never store client fields, property details, or quote payloads locally. Blocked
or corrupt storage must not prevent generation/retrieval. Remove/clear affects
only this browser's list, not SQL records or the validity of existing links.
Opening a link in a fresh browser works without its prior local history.
Current premiums are snapshots, not a guarantee that cover can be purchased.
Acceptance remains disabled. Refreshing/reopening must not submit a new quote.

## Database compatibility

Add `QuoteReference` (uniqueidentifier, uniquely indexed) and `CreatedAt`
(datetime2 UTC) to Properties. Generate new references in the application. The
upgrade backfills legacy references with SQL NEWID() and creation time from the
linked client's CreatedAt (the original submission time). Existing data survives;
historical references have no public enumeration endpoint. Fresh installs and
upgraded databases must end with compatible columns/indexes. The upgrade must be
repeatable and fail transactionally on unexpected partial state.

## Delivery and verification

The 20-commit checklist is in `PLAN.md`. Every behavior change updates this guide
or other docs in the same commit. Verify each layer before the next; real SQL
integration proves uniqueness/upgrade behavior and read-only retrieval, while
Playwright proves private links, reloads, local removal, and failure recovery.

The fresh schema includes comparison metadata. Upgrade existing databases using
`database/upgrades/001-saved-comparisons.sql`; it preserves rows and backfills from
client submission time. CI runs `scripts/verify-comparison-schema.py` against a
disposable database to check fresh/legacy/repeated application and uniqueness.
API and UI implementation follows on `feature/quote-history`.
EF maps the unique reference index and second-precision timestamp; references
are application-generated, so SQL defaults also support standalone SQL inserts.
Persistence returns the saved reference and second-precision UTC time after
commit, including comparisons with no quotes. Repeated submissions remain distinct.
The POST controller now includes this metadata; rating's internal `QuoteResponse`
continues to carry only quotes, keeping storage concerns out of scheme calculations.
`ComparisonLookupService` projects only the saved property and quote snapshot,
without tracking entities or loading client identity. SQL datetime values are
explicitly marked UTC when returned; equal premiums use ordinal name ordering.
The core GET route is implemented by `SavedComparisonsController`, whose only
dependency is the read service. It rejects malformed/all-zero references without
querying SQL and accepts uppercase canonical UUIDs.
Production deployment remains placeholder-only.
