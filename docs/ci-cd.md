# CI/CD

## Current Status

The repository is scaffolded. CI should validate the files that exist today and expand as implementation is added.

## Pull Request CI

The PR workflow runs for pull requests into `dev` and `main`.

Current checks:

- Restore and build the .NET core engine.
- Validate required documentation files exist.
- Validate PHP file syntax when PHP is available in CI.
- Validate PHP Composer metadata.

Future checks:

- .NET unit tests.
- PHP gateway tests.
- Frontend linting and accessibility checks.
- SQL schema validation.
- Container build validation.

## Production Deployment

Production deployment must be triggered only from `main`.

Initial deployment workflow is intentionally a placeholder until Azure targets are created. It should later deploy:

- .NET core engine to Azure App Service or Azure Container Apps.
- PHP gateway to Azure App Service.
- Frontend static assets to the selected hosting target.
- SQL schema changes through a controlled migration path.

## Branch Protections To Configure In GitHub

- Require PRs before merging into `dev` and `main`.
- Require CI checks before merging.
- Block direct pushes to `main`.
- Prefer blocking direct pushes to `dev` once the first baseline is merged.
- Require at least one approval for production PRs into `main`.
