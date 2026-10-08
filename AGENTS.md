# AGENTS.md

## Repo Scope

- Treat `micro-insurtech-brokerage-engine` as its own standalone repository, not as part of the parent `Rewards_App` repo.
- Keep every behavior change documented in `README.md` or `docs/` in the same PR.
- Commit small, coherent changes consistently; do not batch unrelated work.

## Branch And PR Flow

- `main` is production only.
- `dev` is the integration branch for development.
- Start implementation work from `dev` on a `feature/<short-name>` branch.
- Open a PR from the feature branch into `dev` when the change is complete.
- Review the feature PR before merging; include verification results in the PR body.
- Open a separate PR from `dev` into `main` for production deployment and user review.
- Do not push implementation changes directly to `main`.
- Keep documentation updated in the same feature PR as the code or workflow change.

## Current Project Shape

- `core-engine/` is a .NET 8 Web API scaffold; current executable entrypoint is `core-engine/Program.cs`.
- `core-engine/Dtos/` contains the quote request/response contracts used by scheme services.
- `core-engine/Schemes/` contains pure underwriter calculations; keep HTTP and database access out of these classes.
- `core-engine/Services/QuoteOrchestrationService.cs` coordinates postcode enrichment and concurrent underwriter calls.
- `core-engine/Controllers/QuotesController.cs` exposes `POST /api/quotes`.
- `php-gateway/` is a PHP 8.1+ gateway scaffold using PSR-4 namespace `MicroInsurTech\Gateway\` mapped to `src/`.
- `frontend/` is native HTML/CSS/JavaScript, with npm-managed Vitest DOM tests and Playwright browser tests. `app.js` binds the form; `quote-ui.js` owns quote UI behavior.
- `database/schema.sql` contains the SQL Server schema for `Clients`, `Properties`, and `Quotes`.
- `core-engine-tests/` contains xUnit tests for schemes, postcode lookup, orchestration, controllers, and EF Core persistence mapping.

## Verified Commands

- Build the .NET scaffold from repo root: `dotnet build core-engine/MicroInsurTech.CoreEngine.csproj`.
- Build the test project from repo root: `dotnet build core-engine-tests/MicroInsurTech.CoreEngine.Tests.csproj`.
- Run tests when .NET 8 runtime is installed: `dotnet test core-engine-tests/MicroInsurTech.CoreEngine.Tests.csproj`.
- If only .NET 9 is installed, `DOTNET_ROLL_FORWARD=Major dotnet test core-engine-tests/MicroInsurTech.CoreEngine.Tests.csproj` provides a local fallback. Report the runtime used; CI still verifies native .NET 8 with `actions/setup-dotnet@v4`.
- PHP is not guaranteed on local PATH in the current environment; do not claim PHP checks passed unless you run them.
- From `php-gateway/`: `composer install`, `composer validate --strict --no-check-lock`, and `composer test`.
- From `frontend/`: `npm ci` and `npm test` for Vitest; `npx playwright install chromium` and `npm run test:e2e` for browser tests against a prepared fixture stack.
- From repo root: `python3 -m unittest discover -s scripts/tests` for local frontend proxy tests, and `sh scripts/check-docs.sh` for documentation presence.
- Full HTTP/SQL/browser integration: `python3 scripts/run-integration.py` (requires Docker with amd64 Linux support, .NET 8, PHP/cURL, Node, and browser dependencies). See `docs/verification.md`.

## Configuration Notes

- Use `.env.example` as the documented environment contract; never commit real `.env` files.
- Local topology in `docs/setup.md` uses frontend `localhost:3000`, PHP gateway `localhost:8080`, .NET API `localhost:5000`, and SQL Server `localhost,1433`.
- Browser traffic should go through the PHP gateway; do not wire the frontend directly to the .NET API.
- The core API can be exercised directly during backend development, but production browser traffic should still be routed through PHP.

## CI/CD Expectations

- PR checks must include documentation presence checks.
- Deployment should only run from `main` after a `dev` to `main` PR is merged.
- Keep CI config and release workflow changes documented in `docs/ci-cd.md`.
