# Branching Strategy

## Branches

- `main`: production branch.
- `dev`: development integration branch.
- `feature/<short-name>`: short-lived branches for individual changes.

## Pull Request Flow

1. Feature work is opened as a PR into `dev`.
2. `dev` collects reviewed and passing changes.
3. Production releases are opened as PRs from `dev` into `main`.
4. Deployment workflows run only after changes reach `main`.

## Required Checks

- .NET build.
- PHP validation when PHP tooling is available.
- Documentation presence check.
- Future test suites once implementation begins.

## Working Convention

- Keep commits small and coherent.
- Update documentation in the same change when behavior, setup, architecture, or workflow changes.
- Do not commit secrets, local `.env` files, build outputs, or dependency folders.
