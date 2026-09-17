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
- `php-gateway/` is a PHP 8.1+ gateway scaffold using PSR-4 namespace `MicroInsurTech\Gateway\` mapped to `src/`.
- `frontend/` is a plain HTML/CSS/JavaScript scaffold; no package manager is configured yet.
- `database/schema.sql` is still a placeholder; SQL Server schema has not been implemented.
- Business logic is intentionally minimal at this stage.

## Verified Commands

- Build the .NET scaffold from repo root: `dotnet build core-engine/MicroInsurTech.CoreEngine.csproj`.
- PHP is not guaranteed on local PATH in the current environment; do not claim PHP checks passed unless you run them.
- No test projects or frontend package scripts exist yet.

## Configuration Notes

- Use `.env.example` as the documented environment contract; never commit real `.env` files.
- Local topology in `docs/setup.md` uses frontend `localhost:3000`, PHP gateway `localhost:8080`, .NET API `localhost:5000`, and SQL Server `localhost,1433`.
- Browser traffic should go through the PHP gateway; do not wire the frontend directly to the .NET API.

## CI/CD Expectations

- PR checks must include documentation presence checks.
- Deployment should only run from `main` after a `dev` to `main` PR is merged.
- Keep CI config and release workflow changes documented in `docs/ci-cd.md`.
