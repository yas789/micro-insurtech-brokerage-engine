# Branching Strategy

## Branches

- `main`: production branch.
- `dev`: development integration branch.
- `feature/<short-name>`: short-lived branches for individual changes.

## Pull Request Flow

1. Create a `feature/<short-name>` branch from `dev`.
2. Implement the smallest coherent change and update documentation in the same branch.
3. Commit the completed change with a focused message.
4. Open a PR from the feature branch into `dev`.
5. Review the PR and confirm CI checks pass before merging to `dev`.
6. Open a separate PR from `dev` into `main` for production deployment review.
7. Deployment workflows run only after changes reach `main`.

## Required Checks

- .NET build.
- PHP validation when PHP tooling is available.
- Documentation presence check.
- Future test suites once implementation begins.

## Working Convention

- Keep commits small and coherent.
- Update documentation in the same change when behavior, setup, architecture, or workflow changes.
- Do not commit secrets, local `.env` files, build outputs, or dependency folders.
- Do not skip the feature PR into `dev`, even for documentation-only changes.
