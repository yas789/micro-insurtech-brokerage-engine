# Core Orchestration In Natural Language

This file translates the quote API validation and orchestration code into plain English so future implementation work can compare intent against code.

## Incoming Quote Request Validation

Source: `core-engine/Controllers/QuotesController.cs`

When `POST /api/quotes` receives a request:

1. Start with an empty list of validation errors.
2. If no request body is provided, record `Request body is required.` and stop validation immediately.
3. If the client payload is missing, record `Client details are required.`.
4. If the client payload exists, validate the client's fields:
   - If first name is blank or missing, record `Client first name is required.`.
   - If first name is longer than `100` characters after trimming, record `Client first name must be 100 characters or fewer.`.
   - If last name is blank or missing, record `Client last name is required.`.
   - If last name is longer than `100` characters after trimming, record `Client last name must be 100 characters or fewer.`.
   - If email is blank, missing, or does not contain `@`, record `A valid client email is required.`.
   - If email is longer than `254` characters after trimming, record `Client email must be 254 characters or fewer.`.
5. If the property payload is missing, record `Property details are required.` and stop validation immediately.
6. If the property payload exists, validate the property fields:
   - If postcode is blank or missing, record `Property postcode is required.`.
   - If postcode is longer than `16` characters after trimming, record `Property postcode must be 16 characters or fewer.`.
   - If year built is before `1500` or after `2100`, record `Property year built must be between 1500 and 2100.`.
   - If rebuild cost is zero or negative, record `Property rebuild cost must be greater than zero.`.
7. If any errors were recorded, return HTTP `400 Bad Request` with those errors.
8. If no errors were recorded, pass the request to the quote orchestration service.
9. Persist the client, property, and returned quote rows before returning a successful response.

## Quote Orchestration Flow

Source: `core-engine/Services/QuoteOrchestrationService.cs`

When a valid quote request reaches the orchestration service:

1. Confirm property details are present. If not, throw an argument error because the controller should have already rejected the request.
2. Use the submitted postcode to ask the postcode lookup service for a geographical region.
3. Create an enriched property evaluation object by copying the original property details and adding the resolved region.
4. Send the same enriched property evaluation to every registered underwriter service.
5. Run all underwriter calculations concurrently with `Task.WhenAll`.
6. Sort the returned quotes by `PremiumAmount` from lowest to highest.
7. If two quotes have the same premium, sort those quotes by `UnderwriterName` for deterministic output.
8. Return a `QuoteResponse` containing the sorted quotes.

## Persistence Flow

Source: `core-engine/Services/QuotePersistenceService.cs`

After quote orchestration succeeds:

1. Confirm client and property details are present. If not, throw an argument error because the controller should have already rejected the request.
2. Open a database transaction when the database provider supports transactions.
3. Save the client details.
4. Save the property details and link them to the saved client.
5. Save each returned underwriter quote and link it to the saved property.
6. Commit the transaction.
7. If saving fails, the API request fails instead of returning unsaved quotes.

## Postcode Lookup Flow

Source: `core-engine/Services/PostcodesIoLookupService.cs`

When the postcode lookup service is asked for a region:

1. If the postcode is blank or missing, return `null` without making an HTTP call.
2. Trim and URL-encode the postcode.
3. Call `/postcodes/{postcode}` on the configured `POSTCODES_API_BASE_URL`.
4. If the response contains a non-blank region, return that region.
5. If the response is missing, malformed, or has no region, return `null`.
6. If the request is cancelled by the caller, propagate cancellation.
7. If any other lookup error occurs, log a warning and return `null` so quote calculation can continue.
