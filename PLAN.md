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
- [x] 2. Validate trimmed gateway text lengths using UTF-16 storage units.
  Verification: PHPUnit ASCII/Unicode boundary and no-forwarding tests.
  Result: PHPUnit passed (52 tests, 110 assertions).
- [x] 3. Validate trimmed gateway email addresses.
  Verification: PHPUnit whitespace and invalid-email cases.
  Result: PHPUnit passed (58 tests, 126 assertions).
- [x] 4. Strengthen direct core email validation.
  Verification: .NET controller email acceptance/rejection tests.
  Result: Build passed; 34 controller tests passed with DOTNET_ROLL_FORWARD=Major
  on .NET 9. Native .NET 8 verification is deferred to CI.
- [x] 5. Normalize upstream field-keyed validation errors at gateway.
  Verification: PHPUnit realistic ASP.NET binding-error responses.
  Result: PHPUnit passed (60 tests, 129 assertions).
- [x] 6. Display useful errors for non-JSON quote responses.
  Verification: Vitest HTML, empty, malformed response cases.
  Result: npm test passed (19 tests).
- [x] 7. Prevent overlapping submissions per form.
  Verification: Vitest deferred requests and subsequent submission.
  Result: npm test passed (22 tests), including independent forms.
- [x] 8. Distinguish empty quote responses from successful comparisons.
  Verification: Vitest empty/nonempty status assertions.
  Result: npm test passed (23 tests).
- [~] 9. Display unavailable premiums honestly, preserving zero.
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

- Local .NET runtime is 9 only. Isolated runtime download succeeded but the test
  runner still selected the system host; isolated SDK download was interrupted.
  User requested moving on. Local tests use DOTNET_ROLL_FORWARD=Major; CI uses 8.
- Homebrew PHP installation failed before installation; standalone PHP 8.2.32
  downloaded to the harness temporary directory runs local PHPUnit checks.
  Composer dependencies are installed in ignored `php-gateway/vendor/`.
- Full SQL/browser integration requires Docker/amd64 Linux; run in GitHub CI
  if Docker is unavailable locally.
- Review all fifteen commits and complete PR checks before delivery.
