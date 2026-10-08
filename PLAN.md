# Codebase improvement plan

## Goal and baseline

Deliver fifteen small, individually verified commits improving validation, quote
UI reliability, developer guidance, and CI. Started from `origin/dev` at
`7a5623a`, which includes rebuild-cost validation and deferred cover state.
Implementation branch: `feature/codebase-improvements`; PR targets `dev`.
Each behavior/workflow change includes documentation in `docs/` or `README.md`.

## Checklist

- [x] 1. Align gateway year-built range with core (1500–2100).
  Verification: PHPUnit gateway boundary and no-forwarding tests.
  Result: PHPUnit passed (40 tests, 80 assertions).
- [~] 2. Validate trimmed gateway text lengths using UTF-16 storage units.
  Verification: PHPUnit ASCII/Unicode boundary and no-forwarding tests.
- [ ] 3. Validate trimmed gateway email addresses.
  Verification: PHPUnit whitespace and invalid-email cases.
- [ ] 4. Strengthen direct core email validation.
  Verification: .NET controller email acceptance/rejection tests.
- [ ] 5. Normalize upstream field-keyed validation errors at gateway.
  Verification: PHPUnit realistic ASP.NET binding-error responses.
- [ ] 6. Display useful errors for non-JSON quote responses.
  Verification: Vitest HTML, empty, malformed response cases.
- [ ] 7. Prevent overlapping submissions per form.
  Verification: Vitest deferred requests and subsequent submission.
- [ ] 8. Distinguish empty quote responses from successful comparisons.
  Verification: Vitest empty/nonempty status assertions.
- [ ] 9. Display unavailable premiums honestly, preserving zero.
  Verification: Vitest missing/invalid/zero/normal premium cases.
- [ ] 10. Expose results loading through aria-busy.
  Verification: Vitest pending/success/failure DOM assertions.
- [ ] 11. Refresh repository tooling guidance and verification entry points.
  Verification: Cross-check package/scripts; documentation checker.
- [ ] 12. Require docs/verification.md in documentation check.
  Verification: Real repo pass and isolated missing-file failure.
- [ ] 13. Cache npm downloads in frontend and integration CI jobs.
  Verification: Workflow validation and remote CI run.
- [ ] 14. Upload browser diagnostics on integration failures.
  Verification: Workflow validation and failure-artifact exercise.
- [ ] 15. Cancel superseded PR CI runs without cancelling manual runs.
  Verification: Workflow validation and two-update PR exercise.

## Environment and final verification

- Local .NET runtime is 9 only; use an isolated .NET 8 runtime for net8 tests.
- Homebrew PHP installation failed before installation; standalone PHP 8.2.32
  downloaded to the harness temporary directory runs local PHPUnit checks.
  Composer dependencies are installed in ignored `php-gateway/vendor/`.
- Full SQL/browser integration requires Docker/amd64 Linux; run in GitHub CI
  if Docker is unavailable locally.
- Review all fifteen commits and complete PR checks before delivery.
